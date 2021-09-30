using UnityEngine;
using Reccy.ScriptExtensions; // https://github.com/Reccy/UnityScriptExtensions

namespace Reccy.UnityBezierCurve
{
    public class BezierCurve
    {
        private LineSegment m_lineA;
        private LineSegment m_lineB;
        private LineSegment m_lineC;

        private LineSegment m_lineD;
        private LineSegment m_lineE;

        private LineSegment m_lineF;

        private Vector2 m_pointA;
        private Vector2 m_pointB;
        private Vector2 m_pointC;
        private Vector2 m_pointD;

        // LUT = LookUp Table
        private const int LUT_DETAIL = 32;
        private bool m_lutBuilt = false;
        private float[] m_tLut;
        private float[] m_dLut;

        private float m_totalLength;
        public float Length
        {
            get
            {
                BuildLUT();

                return m_totalLength;
            }
        }

        public BezierCurve(Vector2 pointA, Vector2 pointB, Vector2 pointC, Vector2 pointD)
        {
            m_pointA = pointA;
            m_pointB = pointB;
            m_pointC = pointC;
            m_pointD = pointD;

            Init(pointA, pointB, pointC, pointD);
        }

        public BezierCurve(BezierPoint pointA, BezierPoint pointB, BezierPoint pointC, BezierPoint pointD)
        {
            m_pointA = pointA.Point;
            m_pointB = pointB.Point;
            m_pointC = pointC.Point;
            m_pointD = pointD.Point;

            pointA.OnTransformChanged += OnBezierPointAChanged;
            pointB.OnTransformChanged += OnBezierPointBChanged;
            pointC.OnTransformChanged += OnBezierPointCChanged;
            pointD.OnTransformChanged += OnBezierPointDChanged;

            Init(pointA.Point, pointB.Point, pointC.Point, pointD.Point);
        }

        private void Init(Vector2 pointA, Vector2 pointB, Vector2 pointC, Vector2 pointD)
        {
            m_lineA = new LineSegment(pointA, pointB);
            m_lineB = new LineSegment(pointB, pointC);
            m_lineC = new LineSegment(pointC, pointD);

            m_lineD = new LineSegment(pointA, pointB);
            m_lineE = new LineSegment(pointB, pointC);

            m_lineF = new LineSegment(pointA, pointB);

            m_tLut = new float[LUT_DETAIL];
            m_dLut = new float[LUT_DETAIL];
        }

        // Code adapted from Freya Holmer: https://docs.google.com/presentation/d/10XjxscVrm5LprOmG-VB2DltVyQ_QygD26N6XC2iap2A/edit#slide=id.gdefa50559_1_50
        private void BuildLUT()
        {
            if (m_lutBuilt)
                return;

            m_tLut[0] = 0f;
            m_dLut[0] = 0f;
            m_totalLength = 0;
            Vector2 prev = m_pointA;

            for (int i = 1; i < LUT_DETAIL; ++i)
            {
                float t = (float)i / (m_tLut.Length - 1);
                Vector2 pt = Point(t);
                float diff = (prev - pt).magnitude;
                m_totalLength += diff;
                m_tLut[i] = t;
                m_dLut[i] = m_totalLength;
                prev = pt;
            }

            m_lutBuilt = true;
        }

        private void OnBezierPointAChanged(BezierPoint point)
        {
            m_pointA = point.Point;

            m_lineA.Begin = m_pointA;

            m_lutBuilt = false;
        }

        private void OnBezierPointBChanged(BezierPoint point)
        {
            m_pointB = point.Point;

            m_lineA.End = m_pointB;
            m_lineB.Begin = m_pointB;

            m_lutBuilt = false;
        }

        private void OnBezierPointCChanged(BezierPoint point)
        {
            m_pointC = point.Point;

            m_lineB.End = m_pointC;
            m_lineC.Begin = m_pointC;

            m_lutBuilt = false;
        }

        private void OnBezierPointDChanged(BezierPoint point)
        {
            m_pointD = point.Point;

            m_lineC.End = m_pointD;

            m_lutBuilt = false;
        }

        public Vector2 Point(float t)
        {
            m_lineD.Begin = m_lineA.Point(t);
            m_lineD.End = m_lineB.Point(t);

            m_lineE.Begin = m_lineB.Point(t);
            m_lineE.End = m_lineC.Point(t);

            m_lineF.Begin = m_lineD.Point(t);
            m_lineF.End = m_lineE.Point(t);

            return m_lineF.Point(t);
        }

        // Returns approximate distance at the given t value along the curve
        public float Distance(float t)
        {
            BuildLUT();

            return SampleDistance(t);
        }

        // Returns approximate t value at the given distance along the curve
        public float T(float dist)
        {
            BuildLUT();

            return SampleT(dist);
        }

        public Vector2 PointDist(float dist)
        {
            return Point(T(dist));
        }

        public Vector2 Tangent(float t)
        {
            m_lineD.Begin = m_lineA.Point(t);
            m_lineD.End = m_lineB.Point(t);

            m_lineE.Begin = m_lineB.Point(t);
            m_lineE.End = m_lineC.Point(t);

            Vector2 p1 = m_lineD.Point(t);
            Vector2 p2 = m_lineE.Point(t);

            return (p2 - p1).normalized;
        }

        public Vector2 TangentDist(float dist)
        {
            return Tangent(T(dist));
        }

        public Vector2 Normal(float t)
        {
            return Tangent(t).RotatedDeg(-90);
        }

        public Vector2 NormalDist(float dist)
        {
            return Normal(T(dist));
        }

        private float SampleT(float dist)
        {
            int count = m_tLut.Length;

            if (count == 0)
            {
                Debug.LogError("Unable to sample array - it has no elements");
                return 0;
            }

            if (count == 1)
            {
                return m_tLut[0];
            }

            int idLower = 0;
            int idUpper = 1;
            float iFloat = 0;
            for (int i = 0; i < count - 1; ++i)
            {
                float lower = m_dLut[idLower];
                float upper = m_dLut[idUpper];

                if (lower <= dist && dist <= upper)
                {
                    iFloat = Mathf.InverseLerp(lower, upper, dist);
                    break;
                }

                idLower++;
                idUpper++;
            }

            if (idUpper >= count)
            {
                return m_tLut[count - 1];
            }

            if (idLower < 0)
            {
                return m_tLut[0];
            }

            return Mathf.Lerp(m_tLut[idLower], m_tLut[idUpper], iFloat);
        }

        // Source: https://docs.google.com/presentation/d/10XjxscVrm5LprOmG-VB2DltVyQ_QygD26N6XC2iap2A/edit#slide=id.gdefa50559_1_64
        private float SampleDistance(float t)
        {
            int count = m_dLut.Length;

            if (count == 0)
            {
                Debug.LogError("Unable to sample array - it has no elements");
                return 0;
            }

            if (count == 1)
            {
                return m_dLut[0];
            }

            float iFloat = t * (count - 1);
            int idLower = Mathf.FloorToInt(iFloat);
            int idUpper = Mathf.FloorToInt(iFloat + 1);

            if (idUpper >= count)
            {
                return m_dLut[count - 1];
            }

            if (idLower < 0)
            {
                return m_dLut[0];
            }

            return Mathf.Lerp(m_dLut[idLower], m_dLut[idUpper], iFloat - idLower);
        }
    }

}
