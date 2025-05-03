using System;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

////////////////////////////////////////////////
// PLEASE DO NOT MODIFY THIS FILE
////////////////////////////////////////////////

////////////////////////////////////////////////
// For this project, you will only need to use the IndexOfAir
// constant to calculate contributions from refraction ray
////////////////////////////////////////////////
namespace DefaultNamespace
{
    /// <summary>
    /// Stores data of a mesh in the scene.
    /// </summary>
    public class MeshObject
    {
        public const float IndexOfAir = 1.0003f; // You'll probably need to use this
        
        private readonly String _type;
        public readonly GameObject GameObject;
        private readonly Vector3 _pos;
        private readonly float _sphereRadius;
        private Matrix4x4 _local2WorldMat;
        private readonly Material _material;
        private readonly int[] _triangles;
        private readonly Vector3[] _vertices;
        private readonly Vector3[] _normals;

        public MeshObject(String type, GameObject gameObject, float sphereRadius, Matrix4x4 local2WorldMat,
            Vector3 pos, Material mat, int[] triangles, Vector3[] vertices, Vector3[] normals)
        {
            _type = type;
            GameObject = gameObject;
            _sphereRadius = sphereRadius;
            _local2WorldMat = local2WorldMat;
            _pos = pos;
            _material = mat;
            _triangles = triangles;
            _vertices = vertices;
            _normals = normals;
        }
        
        /// <summary>
        /// Convert a position from local (wrt to the current mesh) to world position
        /// </summary>
        /// <param name="myPos">current local position</param>
        /// <returns>world position</returns>
        private Vector3 Local2WorldPos(Vector3 myPos)
        {
            Vector3 v = Vector3.Scale(myPos, _local2WorldMat.lossyScale);
            return RotateByQuaternion(_local2WorldMat.rotation, v) + _pos;
        }

        /// <summary>
        /// Convert a direction from local (wrt to the current mesh) to world direction
        /// </summary>
        /// <param name="myPos">current local direction</param>
        /// <returns>world direction</returns>
        private Vector3 Local2WorldDir(Vector3 myDir)
        {
            return RotateByQuaternion(_local2WorldMat.rotation, myDir);
        }

        /// <summary>
        /// Rotate a vector by quaternion
        /// </summary>
        /// <param name="rot">rotation quaternion</param>
        /// <param name="v">vector</param>
        /// <returns>rotated vector</returns>
        private static Vector3 RotateByQuaternion(Quaternion rot, Vector3 v)
        {
            Vector3 u = new Vector3(rot.x, rot.y, rot.z);
            float s = rot.w;
            return 2.0f * Vector3.Dot(u, v) * u + (s * s - Vector3.Dot(u, u)) * v + 2.0f * s * Vector3.Cross(u, v);
        }
        
        
        // Constants related to intersection calculation
        public const float RayEpsilon = 0.001f;
        public const float NormalEpsilon = 0.00001f;
        public const float EdgeEpsilon = 0.000000001f;

        public struct Intersection
        {
            public Vector3 point { set; get; } // location of intersection
            public float distance { set; get; } // distance from ray origin to intersection
            public Vector3 normal { set; get; } // normal vector at intersection
            public Material material { set; get; } // the material that is intersected
        }

        /// <summary>
        /// Check if a ray intersects a meshObject and return intersection point as out parameter
        /// </summary>
        /// <param name="ray">current ray</param>
        /// <param name="meshObject">current mesh object</param>
        /// <param name="hitPoint">out parameter that holds intersection point</param>
        /// <returns>true if there is an intersection</returns>
        public bool IntersectLocal(Ray ray, MeshObject meshObject, out Intersection hitPoint)
        {
            hitPoint = new Intersection();
            
            // If our object is a sphere, check for sphere intersection
            if (meshObject._type == "Sphere")
            {
                return CheckSphereIntersection(ray, meshObject, out hitPoint);
            }

            // If our object is a triangle mesh, check for triangle mesh intersection
            if (meshObject._type == "TriMeshes")
            {
                return CheckTriangleMeshIntersection(ray, meshObject, out hitPoint);
            }
            return false;
        }

        /// <summary>
        /// Check ray intersection against a sphere object
        /// </summary>
        /// <param name="ray">current ray</param>
        /// <param name="meshObject">current mesh object</param>
        /// <param name="hitPoint">out parameter that holds intersection point</param>
        /// <returns>true if there is an intersection</returns>
        static bool CheckSphereIntersection(Ray ray, MeshObject meshObject, out Intersection hitPoint)
        {
            hitPoint = new Intersection();
            float radius = meshObject._sphereRadius;
            Vector3 center = meshObject._pos;

            // and return true;
            Vector3 d = ray.direction;
            Vector3 P = ray.origin - center;

            // solving quadratic equation
            double a = Vector3.Dot(d, d);
            double b = 2 * Vector3.Dot(P, d);
            double c = Vector3.Dot(P, P) - Math.Pow(radius, 2);
            double delta = Math.Pow(b, 2) - 4 * a * c;


            if (delta < 0)
            {
                return false;
            }

            // first intersection
            double t1 = (-b + Math.Sqrt(delta)) / (2 * a);
            // if t1 is too small then no intersection at all
            if (t1 < RayEpsilon)
            {
                return false;
            }

            // second intersection
            double t2 = (-b - Math.Sqrt(delta)) / (2 * a);
            // if t2 is too small then ignore it and use t1 instead
            if (t2 < RayEpsilon)
            {
                hitPoint.distance = (float)t1;
            }
            else
            {
                hitPoint.distance = (float)t2;
            }

            hitPoint.point = ray.origin + hitPoint.distance * ray.direction;
            hitPoint.normal = Vector3.Normalize((hitPoint.point - center) / radius);
            hitPoint.material = meshObject._material;
            return true;
        }

        /// <summary>
        /// Check ray intersection against a triangle mesh object
        /// </summary>
        /// <param name="ray">current ray</param>
        /// <param name="meshObject">current mesh object</param>
        /// <param name="hitPoint">out parameter that holds intersection point</param>
        /// <returns>true if there is an intersection</returns>
        static bool CheckTriangleMeshIntersection(Ray ray, MeshObject mo, out Intersection hitPoint)
        {
            hitPoint = new Intersection();
            // Distance to hit point
            float min_dist = Mathf.Infinity;

            int[] triangles = mo._triangles;
            Vector3[] verts = mo._vertices;
            Vector3[] norms = mo._normals;

            // Create each of the triangles to test intersection
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Intersection triangleFaceHitPoint;
                if (IntersectTriangleFace(ray,
                        mo.Local2WorldPos(verts[triangles[i]]),
                        mo.Local2WorldPos(verts[triangles[i + 1]]),
                        mo.Local2WorldPos(verts[triangles[i + 2]]),
                        mo.Local2WorldDir(norms[triangles[i]]),
                        mo.Local2WorldDir(norms[triangles[i + 1]]),
                        mo.Local2WorldDir(norms[triangles[i + 2]]),
                        norms.Length > 0,
                        out triangleFaceHitPoint))
                {
                    if (triangleFaceHitPoint.distance < min_dist)
                    {
                        min_dist = triangleFaceHitPoint.distance;
                        hitPoint = triangleFaceHitPoint;
                        hitPoint.material = mo._material;
                    }
                }
            }

            return min_dist < Mathf.Infinity;
        }

        /// <summary>
        /// Check ray intersection against a triangle face
        /// </summary>
        /// <param name="ray">current ray</param>
        /// <param name="a">vertex 1</param>
        /// <param name="b">vertex 2</param>
        /// <param name="c"> vertex 3</param>
        /// <param name="a_n">normal at vertex 1</param>
        /// <param name="b_n">normal at vertex 2</param>
        /// <param name="c_n">normal at vertex 3</param>
        /// <param name="usePerVertexNormals">whether we use per vertex normal</param>
        /// <param name="hitPoint">out parameter that holds intersection point</param>
        /// <returns></returns>
        static bool IntersectTriangleFace(Ray ray, Vector3 a, Vector3 b, Vector3 c, Vector3 a_n, Vector3 b_n,
            Vector3 c_n,
            bool usePerVertexNormals, out Intersection hitPoint)
        {
            Intersection hit = new Intersection();
            hitPoint = hit;
            Vector3 d = ray.direction;
            Vector3 P = ray.origin;

            Vector3 unNormalizedN = Vector3.Cross(b - a, c - a);
            // check if it's a bad triangle
            if (Vector3.Magnitude(unNormalizedN) == 0)
            {
                return false;
            }


            Vector3 n = Vector3.Normalize(unNormalizedN);
            if (Mathf.Abs(Vector3.Dot(n, d)) < NormalEpsilon)
            {
                return false;
            }

            double t = Vector3.Dot(n, a - P) / Vector3.Dot(n, d);
            if (t < RayEpsilon)
            {
                return false;
            }

            hitPoint.distance = (float)t;
            // get the intersection by plugging t into the ray equation
            Vector3 Q = ray.GetPoint(hitPoint.distance);
            double alpha = Vector3.Dot(Vector3.Cross(c - b, Q - b), n) / Vector3.Dot(unNormalizedN, n);
            double beta = Vector3.Dot(Vector3.Cross(a - c, Q - c), n) / Vector3.Dot(unNormalizedN, n);
            double gamma = 1 - (alpha + beta);


            if (alpha < -EdgeEpsilon || beta < -EdgeEpsilon || alpha > 1 || beta > 1 || gamma < -EdgeEpsilon)
            {
                return false;
            }

            if (usePerVertexNormals)
            {
                // compute and use the Phong-interpolated normal at the intersection point.
                Vector3 blinnPhongInterpolatedNormal =
                    (float)alpha * a_n + (float)beta * b_n + (float)gamma * c_n;
                hitPoint.normal = Vector3.Normalize(blinnPhongInterpolatedNormal);
            }
            else
            {
                // use the normal of the triangle's supporting plane.
                hitPoint.normal = n;
            }

            hitPoint.point = ray.origin + hitPoint.distance * ray.direction;
            return true;
        }
    }
}