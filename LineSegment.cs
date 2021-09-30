using UnityEngine;

namespace Reccy.UnityBezierCurve
{
    public class LineSegment
    {
        private Vector2 m_begin;
        private Vector2 m_end;

        public Vector2 Begin
        {
            get => m_begin;
            set { m_begin = value; }
        }

        public Vector2 End
        {
            get => m_end;
            set { m_end = value; }
        }

        public LineSegment(Vector2 begin, Vector2 end)
        {
            m_begin = begin;
            m_end = end;
        }

        public Vector2 Point(float t)
        {
            return Vector2.Lerp(m_begin, m_end, t);
        }

        public Vector2 NegPoint(float t)
        {
            return Vector2.Lerp(m_end, m_begin, t);
        }
    }
}
