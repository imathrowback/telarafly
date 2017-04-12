using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
    public class NiBSplineBasisData : NIFObject
    {
        const int DEGREE = 3;
        //  The number of control points of the B-spline (number of frames of animation plus degree of B-spline minus one).
        public int m_iQuantity;
        public float[] m_afValue;
        //private float m_fLastTime=-1;
        private int m_iMax;
        private int m_iMin;

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            m_afValue = new float[DEGREE+1];
            this.m_iQuantity = (int)ds.readUInt();
        }

        internal void compute3(float fTime, out int iMin, out int iMax)
        {
                int iQm3 = m_iQuantity - 3;
                int i;
            /*
                if (fTime == m_fLastTime)
                {
                    // The m_afValue[] elements are already correct.
                    iMin = m_iMin;
                    iMax = m_iMax;
                    return;
                }
                */
                //m_fLastTime = fTime;

                // Determine the maximum index affected by local control.  Open
                // splines clamp to [0,1].
                if (fTime < 1.0f)
                {
                    i = 3 + (int)(fTime * (float)iQm3);
                }
                else // fTime == 1.0f
                {
                    i = m_iQuantity - 1;
                }


                m_iMin = iMin = i - 3;
                m_iMax = iMax = i;

                float fQm3 = (float)iQm3;
                float fT = fQm3 * fTime;

                const float fOneThird = 1.0f / 3.0f;

                if (m_iQuantity >= 7)
                {
                    int iQm2 = iQm3 + 1, iQm1 = iQm2 + 1;

                    float fG0 = (i > 5 ? (float)(i - 5) : 0.0f);
                    float fG1 = (i > 4 ? (float)(i - 4) : 0.0f);
                    float fG2 = (float)(i - 3);
                    float fG3 = (float)(i - 2);
                    float fG4 = (i < iQm2 ? (float)(i - 1) : (float)iQm3);
                    float fG5 = (i < iQm3 ? (float)i : (float)iQm3);

                    float fInvG3mG1 = (i == 3 ? 1.0f : 0.5f);
                    float fInvG4mG2 = (i == iQm1 ? 1.0f : 0.5f);
                    float fInvG3mG0 = (i == 3 ? 1.0f : (i == 4 ? 0.5f : fOneThird));
                    float fInvG4mG1 = (i == 3 || i == iQm1 ? 0.5f : fOneThird);
                    float fInvG5mG2 = (i == iQm1 ? 1.0f :
                        (i == iQm2 ? 0.5f : fOneThird));

                    float fTmG0 = fT - fG0;
                    float fTmG1 = fT - fG1;
                    float fTmG2 = fT - fG2;
                    float fG3mT = fG3 - fT;
                    float fG4mT = fG4 - fT;
                    float fG5mT = fG5 - fT;

                    float fExpr0 = fG3mT * fInvG3mG1;
                    float fExpr1 = fTmG2 * fInvG4mG2;
                    float fExpr2 = fInvG3mG0 * fG3mT * fExpr0;
                    float fExpr3 = fInvG4mG1 * (fTmG1 * fExpr0 + fG4mT * fExpr1);
                    float fExpr4 = fInvG5mG2 * fTmG2 * fExpr1;

                    m_afValue[0] = fG3mT * fExpr2;
                    m_afValue[1] = fTmG0 * fExpr2 + fG4mT * fExpr3;
                    m_afValue[2] = fTmG1 * fExpr3 + fG5mT * fExpr4;
                    m_afValue[3] = fTmG2 * fExpr4;
                    return;
                }

                if (m_iQuantity == 6)
                {
                    if (i == 3)
                    {
                        float f1mT = 1.0f - fT;
                        float f2mT = 2.0f - fT;
                        float f3mT = 3.0f - fT;
                        float fHalfT = 0.5f * fT;
                        float f1mTSqr = f1mT * f1mT;
                        float fExpr0 = 0.5f * (fT * f1mT + f2mT * fHalfT);
                        float fExpr1 = fOneThird * fT * fHalfT;

                        m_afValue[0] = f1mT * f1mTSqr;
                        m_afValue[1] = fT * f1mTSqr + f2mT * fExpr0;
                        m_afValue[2] = fT * fExpr0 + f3mT * fExpr1;
                        m_afValue[3] = fT * fExpr1;
                    }
                    else if (i == 4)
                    {
                        float fTm1 = fT - 1.0f;
                        float f2mT = 2.0f - fT;
                        float f3mT = 3.0f - fT;
                        float fHalfT = 0.5f * fT;
                        float f1mHalfT = 1.0f - fHalfT;
                        float fHalfTm1 = 0.5f * fTm1;
                        float f1mHalfTSqr = f1mHalfT * f1mHalfT;
                        float fExpr = fOneThird * (fT * f1mHalfT + f3mT * fHalfTm1);
                        float fHalfTm1Sqr = fHalfTm1 * fHalfTm1;

                        m_afValue[0] = f2mT * f1mHalfTSqr;
                        m_afValue[1] = fT * f1mHalfTSqr + f3mT * fExpr;
                        m_afValue[2] = fT * fExpr + f3mT * fHalfTm1Sqr;
                        m_afValue[3] = fTm1 * fHalfTm1Sqr;
                    }
                    else  // i == 5
                    {
                        float fTm1 = fT - 1.0f;
                        float fTm2 = fT - 2.0f;
                        float f3mT = 3.0f - fT;
                        float fHalf3mT = 0.5f * f3mT;
                        float fTm2Sqr = fTm2 * fTm2;
                        float fExpr0 = fOneThird * f3mT * fHalf3mT;
                        float fExpr1 = 0.5f * (fTm1 * fHalf3mT + f3mT * fTm2);

                        m_afValue[0] = f3mT * fExpr0;
                        m_afValue[1] = fT * fExpr0 + f3mT * fExpr1;
                        m_afValue[2] = fTm1 * fExpr1 + f3mT * fTm2Sqr;
                        m_afValue[3] = fTm2 * fTm2Sqr;
                    }
                    return;
                }

                if (m_iQuantity == 5)
                {
                    if (i == 3)
                    {
                        float f1mT = 1.0f - fT;
                        float f2mT = 2.0f - fT;
                        float fHalfT = 0.5f * fT;
                        float f1mTSqr = f1mT * f1mT;
                        float fExpr = 0.5f * (fT * f1mT + f2mT * fHalfT);
                        float fHalfTSqr = fHalfT * fHalfT;

                        m_afValue[0] = f1mT * f1mTSqr;
                        m_afValue[1] = fT * f1mTSqr + f2mT * fExpr;
                        m_afValue[2] = fT * fExpr + f2mT * fHalfTSqr;
                        m_afValue[3] = fT * fHalfTSqr;
                    }
                    else  // i == 4
                    {
                        float fTm1 = fT - 1.0f;
                        float f2mT = 2.0f - fT;
                        float fHalfT = 0.5f * fT;
                        float fTm1Sqr = fTm1 * fTm1;
                        float f1mHalfT = 1.0f - fHalfT;
                        float f1mHalfTSqr = f1mHalfT * f1mHalfT;
                        float fExpr = f1mHalfT * (fHalfT + fTm1);

                        m_afValue[0] = f2mT * f1mHalfTSqr;
                        m_afValue[1] = fT * f1mHalfTSqr + f2mT * fExpr;
                        m_afValue[2] = fT * fExpr + f2mT * fTm1Sqr;
                        m_afValue[3] = fTm1 * fTm1Sqr;
                    }
                    return;
                }

                if (m_iQuantity == 4)
                {
                    // i == 3
                    float f1mT = 1.0f - fT;
                    float fTSqr = fT * fT;
                    float f1mTSqr = f1mT * f1mT;

                    m_afValue[0] = f1mT * f1mTSqr;
                    m_afValue[1] = 3.0f * fT * f1mTSqr;
                    m_afValue[2] = 3.0f * fTSqr * f1mT;
                    m_afValue[3] = fT * fTSqr;
                    return;
                }

            }


        internal void computeNon3(float fTime, out int iMin, out int iMax)
        {
            /*
            if (fTime == m_fLastTime)
            {
                // The m_afValue[] elements are already correct.
                iMin = m_iMin;
                iMax = m_iMax;
                return;
            }
            m_fLastTime = fTime;
            */
            // Use scaled time and scaled knots so that 1/(Q-D) does not need to
            // be explicitly stored by the class object.  Determine the extreme
            // indices affected by local control.
            float fQmD = (float)(m_iQuantity - DEGREE);
            float fT;
            if (fTime <= (float)0.0)
            {
                fT = (float)0.0;
                iMin = 0;
                iMax = DEGREE;
            }
            else if (fTime >= (float)1.0)
            {
                fT = fQmD;
                iMax = m_iQuantity - 1;
                iMin = iMax - DEGREE;
            }
            else
            {
                fT = fQmD * fTime;
                iMin = (int)fT;
                iMax = iMin + DEGREE;
            }

            // Cache the indices for use by systems sharing the basis object.
            m_iMin = iMin;
            m_iMax = iMax;

            // Precompute knots to eliminate the need for a function GetKnot(...).
            float []afKnot = new float[2 * DEGREE];
            for (int i0 = 0, i1 = iMax + 1 - DEGREE; i0 < 2 * DEGREE; i0++, i1++)
            {
                if (i1 <= DEGREE)
                    afKnot[i0] = (float)0.0;
                else if (i1 >= m_iQuantity)
                    afKnot[i0] = fQmD;
                else
                    afKnot[i0] = (float)(i1 - DEGREE);
            }

            // Initialize the basis function evaluation table.  The first DEGREE-1
            // entries are zero, but they do not have to be set explicitly.
            m_afValue[DEGREE] = (float)1.0;

            // Update the basis function evaluation table, each iteration overwriting
            // the results from the previous iteration.
            for (int iRow = DEGREE - 1; iRow >= 0; iRow--)
            {
                int iK0 = DEGREE, iK1 = iRow;
                float fKnot0 = afKnot[iK0], fKnot1 = afKnot[iK1];
                float fInvDenom = ((float)1.0) / (fKnot0 - fKnot1);
                float fC1 = (fKnot0 - fT) * fInvDenom, fC0;
                m_afValue[iRow] = fC1 * m_afValue[iRow + 1];

                for (int iCol = iRow + 1; iCol < DEGREE; iCol++)
                {
                    fC0 = (fT - fKnot1) * fInvDenom;
                    m_afValue[iCol] *= fC0;

                    fKnot0 = afKnot[++iK0];
                    fKnot1 = afKnot[++iK1];
                    fInvDenom = ((float)1.0) / (fKnot0 - fKnot1);
                    fC1 = (fKnot0 - fT) * fInvDenom;
                    m_afValue[iCol] += fC1 * m_afValue[iCol + 1];
                }

                fC0 = (fT - fKnot1) * fInvDenom;
                m_afValue[DEGREE] *= fC0;
            }


        }
    }
}
