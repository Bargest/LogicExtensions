﻿// Copyright 2010 the V8 project authors. All rights reserved.
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
//       copyright notice, this list of conditions and the following
//       disclaimer in the documentation and/or other materials provided
//       with the distribution.
//     * Neither the name of Google Inc. nor the names of its
//       contributors may be used to endorse or promote products derived
//       from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

// Ported to Java from Mozilla's version of V8-dtoa by Hannes Wallnoefer.
// The original revision was 67d1049b0bf9 from the mozilla-central tree.

using System.Diagnostics;

namespace Jint.Native.Number.Dtoa
{
    /// <summary>
    /// Helper functions for doubles.
    /// </summary>
    internal class DoubleHelper
    {
        internal const ulong KExponentMask = 0x7FF0000000000000L;
        internal const ulong KSignificandMask = 0x000FFFFFFFFFFFFFL;
        private const ulong KHiddenBit = 0x0010000000000000L;

        private static DiyFp AsDiyFp(ulong d64)
        {
            return new DiyFp(Significand(d64), Exponent(d64));
        }

        // this->Significand() must not be 0.
        internal static DiyFp AsNormalizedDiyFp(ulong d64)
        {
            ulong f = Significand(d64);
            int e = Exponent(d64);


            // The current double could be a denormal.
            while ((f & KHiddenBit) == 0)
            {
                f <<= 1;
                e--;
            }
            // Do the final shifts in one go. Don't forget the hidden bit (the '-1').
            f <<= DiyFp.KSignificandSize - KSignificandSize - 1;
            e -= DiyFp.KSignificandSize - KSignificandSize - 1;
            return new DiyFp(f, e);
        }

        internal static int Exponent(ulong d64)
        {
            if (IsDenormal(d64)) return KDenormalExponent;

            int biasedE = (int) ((d64 & KExponentMask).UnsignedShift(KSignificandSize) & 0xffffffffL);
            return biasedE - KExponentBias;
        }

        internal static int NormalizedExponent(ulong significand, int exponent)
        {
            while ((significand & KHiddenBit) == 0)
            {
                significand = significand << 1;
                exponent = exponent - 1;
            }
            return exponent;
        }

        internal static ulong Significand(ulong d64)
        {
            ulong significand = d64 & KSignificandMask;
            if (!IsDenormal(d64))
            {
                return significand + KHiddenBit;
            }

            return significand;
        }

        // Returns true if the double is a denormal.
        private static bool IsDenormal(ulong d64)
        {
            return (d64 & KExponentMask) == 0L;
        }

        // We consider denormals not to be special.
        // Hence only Infinity and NaN are special.
        private static bool IsSpecial(ulong d64)
        {
            return (d64 & KExponentMask) == KExponentMask;
        }

        internal readonly struct NormalizedBoundariesResult
        {
            public NormalizedBoundariesResult(DiyFp minus, DiyFp plus)
            {
                Minus = minus;
                Plus = plus;
            }

            internal readonly DiyFp Minus;
            internal readonly DiyFp Plus;
        }

        // Returns the two boundaries of first argument.
        // The bigger boundary (m_plus) is normalized. The lower boundary has the same
        // exponent as m_plus.
        internal static NormalizedBoundariesResult NormalizedBoundaries(ulong d64)
        {
            DiyFp v = AsDiyFp(d64);
            bool significandIsZero = (v.F == KHiddenBit);
            var mPlus = DiyFp.Normalize((v.F << 1) + 1, v.E - 1);
            DiyFp mMinus;
            if (significandIsZero && v.E != KDenormalExponent)
            {
                // The boundary is closer. Think of v = 1000e10 and v- = 9999e9.
                // Then the boundary (== (v - v-)/2) is not just at a distance of 1e9 but
                // at a distance of 1e8.
                // The only exception is for the smallest normal: the largest denormal is
                // at the same distance as its successor.
                // Note: denormals have the same exponent as the smallest normals.
                mMinus = new DiyFp((v.F << 2) - 1, v.E - 2);
            }
            else
            {
                mMinus = new DiyFp((v.F << 1) - 1, v.E - 1);
            }
            mMinus = new DiyFp(mMinus.F << (mMinus.E - mPlus.E), mPlus.E);
            return new NormalizedBoundariesResult(mMinus, mPlus);
        }

        private const int KSignificandSize = 52; // Excludes the hidden bit.
        private const int KExponentBias = 0x3FF + KSignificandSize;
        private const int KDenormalExponent = -KExponentBias + 1;
    }
}