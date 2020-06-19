using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Runtime;
using Logic.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logic.Blocks.Api
{
    public class CpuMathExt : ApiDescription
    {
        public override List<CpuApiProperty> InstanceFields => null;
        public override List<CpuApiProperty> StaticFields => new List<CpuApiProperty>
        {
            new CpuApiFunc("newton", false, "numerical solver",
                new Dictionary<string, ArgInfo>{
                    { "func", new ArgInfo("func", "The function whose zero is wanted. It must be a function of a single variable") },
                    { "x0", new ArgInfo("float", "An initial estimate of the zero that should be somewhere near the actual zero.") },
                    { "fprime", new ArgInfo("func", "(optional) The derivative of the function when available.") },
                    { "tol", new ArgInfo("float", "(optional) The allowable error of the zero value.") },
                    { "maxiter", new ArgInfo("int", "(optional) Maximum number of iterations.") },
                    { "fprime2", new ArgInfo("func", "(optional) The second order derivative of the function when available.") },
                    { "x1", new ArgInfo("float", "(optional) Estimate of the zero. Used if `fprime` is not provided.") },
                    { "rtol", new ArgInfo("flot", "(optional) Tolerance (relative) for termination.") },
                    { "full_output", new ArgInfo("bool", "(optional) Return just value (false) or object description (true).") }
                },
                (c) => ((JsValue ctx, JsValue[] x) => Newton(c.Interp, ctx, x))
            ),
        };

        public bool WithinTol(float x, float y, float atol, float rtol)
        {
            return Math.Abs(x - y) <= atol + rtol * Math.Abs(y);
        }

        // Find a zero of a real or complex function using the Newton-Raphson
        // (or secant or Halley’s) method.
        //
        // Math.newton(func, x0, fprime=null, tol=1.48e-08, maxiter=50, fprime2=null,
        // x1, rtol=0.0, full_output=false)
        //
        // Mimicks scipy's implementation of scipy.optimize.newton
        // 
        // Parameters
        // func: function
        //      The function whose zero is wanted. It must be a function of a
        //      single variable
        // x0: float
        //      An initial estimate of the zero that should be somewhere near
        //     the actual zero.
        // fprime : function, optional
        //      The derivative of the function when available and convenient. If it
        //      is null (default), then the secant method is used.
        // tol : float, optional
        //      The allowable error of the zero value.
        // maxiter : int, optional
        //      Maximum number of iterations.
        // fprime2 : function, optional
        //      The second order derivative of the function when available and
        //      convenient. If it is null (default), then the normal Newton-Raphson
        //      or the secant method is used. If it is not null, then Halley's method
        //      is used.
        // x1 : float, optional
        //      Another estimate of the zero that should be somewhere near the
        //      actual zero. Used if `fprime` is not provided.
        // rtol : float, optional
        //      Tolerance (relative) for termination.
        // full_output : bool, optional
        //      If `full_output` is false (default), the root is returned.
        //      If true, the dictionary {{"root": root}, {"converged": true/false},
        //      {"iter": numIter}} is returned.
        public JsValue Newton(Engine eng, JsValue ctx, JsValue[] x)
        {
            int l = x.Length;

            // Arguments and their default values:
            FunctionInstance func;
            float x0;
            JsValue fprime = null;
            float tol = 1.48e-08F;
            long maxiter = 50;
            JsValue fprime2 = null;
            float x1 = 0;
            float rtol = 0.0F;
            bool full_output = false;

            bool x1Provided = false;
            JsValue[] args = new JsValue[1];

            if (l < 2)
                throw new Exception("Invalid value");

            // Conditionally initialize the arguments
            void Parse()
            {
                int curArgIndex = 0;

                func = x[curArgIndex++] as FunctionInstance;

                if (!BlockUtils.TryGetFloat(x[curArgIndex++], out x0))
                    throw new Exception("Invalid value");

                if (curArgIndex >= l)
                    return;
                fprime = x[curArgIndex++];

                if (curArgIndex >= l)
                    return;
                if (!BlockUtils.TryGetFloat(x[curArgIndex++], out tol))
                    throw new Exception("Invalid value");

                if (curArgIndex >= l)
                    return;
                if (!BlockUtils.TryGetLong(x[curArgIndex++], out maxiter))
                    throw new Exception("Invalid value");

                if (curArgIndex >= l)
                    return;
                fprime2 = x[curArgIndex++];

                if (curArgIndex >= l)
                    return;
                x1Provided = BlockUtils.TryGetFloat(x[curArgIndex++], out x1);

                if (curArgIndex >= l)
                    return;
                if (!BlockUtils.TryGetFloat(x[curArgIndex++], out rtol))
                    throw new Exception("Invalid value");

                if (curArgIndex >= l)
                    return;
                full_output = BlockUtils.GetBool(x[curArgIndex++]);
            }

            Parse();

            if (tol <= 0)
                throw new Exception("tol too small (" + tol + " <= 0)");
            if (maxiter < 1)
                throw new Exception("maxiter must be greater than 0");
            float p0 = x0;
            long itr = 0;
            float p = 0;
            if (fprime is FunctionInstance fprimeFunc)
            {
                // Newton - Raphson method
                for (; itr < maxiter; ++itr)
                {
                    // first evaluate fval
                    args[0] = p0;
                    float fval = (float)TypeConverter.ToNumber(func.Call(ctx, args));
                    // if fval is 0, a root has been found, then terminate
                    if (fval == 0)
                        return _newton_result_select(eng, full_output, p0, itr, converged: true);
                    float fder = (float)TypeConverter.ToNumber(fprimeFunc.Call(ctx, args));
                    // stop iterating if the derivative is zero
                    if (fder == 0)
                        return _newton_result_select(eng, full_output, p0, itr + 1, converged: false);

                    // Newton step
                    float newton_step = fval / fder;
                    if (fprime2 is FunctionInstance fp2func)
                    {
                        float fder2 = (float)TypeConverter.ToNumber(fp2func.Call(ctx, args));
                        // Halley's method:
                        // newton_step /= (1.0 - 0.5 * newton_step * fder2 / fder)
                        // Only do it if denominator stays close enough to 1
                        // Rationale:  If 1-adj < 0, then Halley sends x in the
                        // opposite direction to Newton.  Doesn't happen if x is close
                        // enough to root.
                        float adj = newton_step * fder2 / fder / 2;
                        if (Math.Abs(adj) < 1)
                            newton_step /= 1.0F - adj;
                    }
                    p = p0 - newton_step;
                    if (WithinTol(p, p0, atol: tol, rtol: rtol))
                        return _newton_result_select(eng, full_output, p, itr + 1, converged: true);
                    p0 = p;
                }
            }
            else
            {
                // secant method
                float p1, q0, q1;
                if (x1Provided)
                {
                    if (x1 == x0)
                        throw new Exception("x1 and x0 must be different");
                    p1 = x1;
                }
                else
                {
                    float eps = 1e-4F;
                    p1 = x0 * (1 + eps);
                    p1 += (p1 >= 0 ? eps : -eps);
                }
                args[0] = p0;
                q0 = (float)TypeConverter.ToNumber(func.Call(ctx, args));
                args[0] = p1;
                q1 = (float)TypeConverter.ToNumber(func.Call(ctx, args));
                if (Math.Abs(q1) < Math.Abs(q0))
                {
                    float temp = q1;
                    q1 = q0;
                    q0 = temp;

                    temp = p0;
                    p0 = p1;
                    p1 = temp;
                }
                for (; itr < maxiter; ++itr)
                {
                    if (q0 == q1)
                    {
                        p = (p1 + p0) / 2.0F;
                        if (p1 != p0)
                            return _newton_result_select(eng, full_output, p, itr + 1, converged: false);
                        else
                            return _newton_result_select(eng, full_output, p, itr + 1, converged: true);

                    }
                    else
                    {
                        // Secant Step
                        if (Math.Abs(q1) > Math.Abs(q0))
                            p = (-q0 / q1 * p1 + p0) / (1.0F - q0 / q1);
                        else
                            p = (-q1 / q0 * p0 + p1) / (1.0F - q1 / q0);
                    }
                    if (WithinTol(p, p1, atol: tol, rtol: rtol))
                        return _newton_result_select(eng, full_output, p, itr + 1, converged: true);

                    p0 = p1;
                    q0 = q1;
                    p1 = p;
                    args[0] = p1;
                    q1 = (float)TypeConverter.ToNumber(func.Call(ctx, args));
                }
            }
            return _newton_result_select(eng, full_output, p, itr + 1, converged: false);
        }

        private JsValue _newton_result_select(Engine eng, bool full_output, float p0,
            long itr, bool converged)
        {
            if (full_output)
            {
                return JsValue.FromObject(eng, new Dictionary<string, object>()
                {
                    {"root", p0 },
                    {"iter", itr },
                    {"converged", converged }
                });
            }
            else
                return p0;
        }
    }
}
