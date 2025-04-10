using System;
using UnityEngine;

namespace Polygon2D {
    public struct Edge : IEquatable<Edge> {
        public Vector2 A {get; private set;}
        public Vector2 B {get; private set;}

        public Edge(Vector2 a, Vector2 b) {
            A = a;
            B = b;
        }

        public override bool Equals(object obj) => obj!=null && obj is Edge other && this.Equals(other);

        public bool Equals(Edge edge) => (A == edge.A && B == edge.B) || (A == edge.B && B == edge.A);

        public override int GetHashCode() {
            var delta = A-B;
            if(delta.x < 0 || (delta.x == 0 && delta.y < 0)) return (A, B).GetHashCode();
            else return (B, A).GetHashCode();
        }

        public static bool operator ==(Edge lhs, Edge rhs) => lhs.Equals(rhs);

        public static bool operator !=(Edge lhs, Edge rhs) => !(lhs == rhs);

        public readonly float SqrMagnitude => Vector2.SqrMagnitude(A-B);

        public override string ToString()
        {
            return A.ToString() + "," + B.ToString();
        }
    }
}