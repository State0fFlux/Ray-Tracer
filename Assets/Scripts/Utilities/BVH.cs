/***
Scratchapixel's Bounding Volume Hierarchy implementation
https://www.scratchapixel.com/lessons/advanced-rendering/introduction-acceleration-structure/
***/
using System.Collections.Generic;
using DefaultNamespace;
using Priority_Queue;
using UnityEngine;
using static DefaultNamespace.MeshObject;
using Vector3 = UnityEngine.Vector3;

public class BVH
{
    private static readonly int kNumPlaneSetNormals = 7;

    private static readonly Vector3[] PlaneSetNormals =
    {
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, 1),
        new Vector3(Mathf.Sqrt(3) / 3f, Mathf.Sqrt(3) / 3f, Mathf.Sqrt(3) / 3f),
        new Vector3(-Mathf.Sqrt(3) / 3f, Mathf.Sqrt(3) / 3f, Mathf.Sqrt(3) / 3f),
        new Vector3(-Mathf.Sqrt(3) / 3f, -Mathf.Sqrt(3) / 3f, Mathf.Sqrt(3) / 3f),
        new Vector3(Mathf.Sqrt(3) / 3f, -Mathf.Sqrt(3) / 3f, Mathf.Sqrt(3) / 3f)
    };

    private static Octree _octree;


    public BVH(List<MeshObject> meshObjects)
    {
        Extents
            sceneExtents =
                new Extents(); // that's the extent of the entire scene which we need to compute for the octree 
        var extentsList = new Extents[meshObjects.Count];
        for (int i = 0; i < meshObjects.Count; i++)
        {
            extentsList[i] = new Extents();
            for (int j = 0; j < kNumPlaneSetNormals; j++)
            {
                foreach (Vector3 vertexPos in meshObjects[i].GameObject.GetComponent<MeshFilter>().mesh.vertices)
                {
                    float d = Vector3.Dot(PlaneSetNormals[j],
                        meshObjects[i].GameObject.transform.TransformPoint(vertexPos));
                    // set dNEar and dFar
                    if (d < extentsList[i].D[j, 0]) extentsList[i].D[j, 0] = d;
                    if (d > extentsList[i].D[j, 1]) extentsList[i].D[j, 1] = d;
                }
            }

            sceneExtents.ExtendBy(extentsList[i]); // expand the scene extent of this object's extent 
            extentsList[i].MeshObject =
                meshObjects[i]; // the extent itself needs to keep a pointer to the object its holds 
        }

        _octree = new Octree(sceneExtents);

        for (int i = 0; i < meshObjects.Count; i++)
        {
            _octree.Insert(extentsList[i]);
        }

        _octree.Build();
    }

    // Checks if a ray hits an object
    public bool IntersectBoundingBox(Ray ray, out Intersection hit)
    {
        float tHit = Mathf.Infinity;
        hit = new Intersection
        {
            distance = Mathf.Infinity
        };
        float[] precomputedNumerator = new float[kNumPlaneSetNormals];
        float[] precomputedDenominator = new float[kNumPlaneSetNormals];
        for (int i = 0; i < kNumPlaneSetNormals; ++i)
        {
            precomputedNumerator[i] = Vector3.Dot(PlaneSetNormals[i], ray.origin);
            precomputedDenominator[i] = Vector3.Dot(PlaneSetNormals[i], ray.direction);
        }

        float tNear = 0;
        if (!_octree.Root.NodeExtents.Intersect(precomputedNumerator, precomputedDenominator, out tNear,
                out var tFar) ||
            tFar < 0)
            return false;

        tHit = tFar;

        var queue = new FastPriorityQueue<Octree.QueueElement>(100);
        queue.Enqueue(new Octree.QueueElement(_octree.Root, 0), 0);
        while (queue.Count != 0 && queue.First.t < hit.distance)
        {
            OctreeNode node = queue.Dequeue().node;
            if (node.IsLeaf)
            {
                foreach (Extents e in node.NodeExtentsList)
                {
                    Intersection hit2 = new Intersection();
                    if (e.MeshObject.IntersectLocal(ray, e.MeshObject, out hit2) && hit2.distance < hit.distance)
                    {
                        hit = hit2;
                    }
                }
            }
            else
            {
                for (int i = 0; i < 8; ++i)
                {
                    if (node.Child[i] != null)
                    {
                        float tFarChild = tFar;
                        if (!node.Child[i].NodeExtents.Intersect(precomputedNumerator, precomputedDenominator,
                            out var tNearChild, out tFarChild)) continue;
                        float t = (tNearChild < 0 && tFarChild >= 0) ? tFarChild : tNearChild;
                        queue.Enqueue(new Octree.QueueElement(node.Child[i], t), t);
                    }
                }
            }
        }

        return hit.distance < Mathf.Infinity;
    }

    public class Extents
    {
        public readonly float[,] D;
        public MeshObject MeshObject;

        public Extents()
        {
            D = new float[kNumPlaneSetNormals, 2];
            for (int i = 0; i < kNumPlaneSetNormals; i++)
            {
                D[i, 0] = Mathf.Infinity;
                D[i, 1] = -Mathf.Infinity;
            }
        }

        public void ExtendBy(Extents e)
        {
            for (int i = 0; i < kNumPlaneSetNormals; i++)
            {
                if (e.D[i, 0] < D[i, 0]) D[i, 0] = e.D[i, 0];
                if (e.D[i, 1] > D[i, 1]) D[i, 1] = e.D[i, 1];
            }
        }

        public Vector3 Centroid()
        {
            return new Vector3(
                D[0, 0] + D[0, 1] * 0.5f,
                D[1, 0] + D[1, 1] * 0.5f,
                D[2, 0] + D[2, 1] * 0.5f);
        }

        public bool Intersect(float[] precomputedNumerator, float[] precomputedDenominator,
            out float tNear,
            out float tFar)
        {
            tNear = -Mathf.Infinity;
            tFar = Mathf.Infinity;
            for (int i = 0; i < kNumPlaneSetNormals; ++i)
            {
                float tNearExtents = (D[i, 0] - precomputedNumerator[i]) / precomputedDenominator[i];
                float tFarExtents = (D[i, 1] - precomputedNumerator[i]) / precomputedDenominator[i];
                if (precomputedDenominator[i] < 0)
                {
                    float temp = tNearExtents;
                    tNearExtents = tFarExtents;
                    tFarExtents = temp;
                }

                if (tNearExtents > tNear)
                {
                    tNear = tNearExtents;
                }

                if (tFarExtents < tFar) tFar = tFarExtents;
                if (tNear > tFar)
                {
                    return false;
                }
            }

            return true;
        }
    };

    public class OctreeNode
    {
        public readonly OctreeNode[] Child;
        public readonly List<Extents> NodeExtentsList; // pointer to the objects extents 
        public readonly Extents NodeExtents; // extents of the octree node itself 
        public bool IsLeaf;

        public OctreeNode()
        {
            Child = new OctreeNode[8];
            IsLeaf = true;
            NodeExtents = new Extents();
            NodeExtentsList = new List<Extents>();
        }
    }

    private struct Octree
    {
        private readonly Vector3[] _bbox;
        public readonly OctreeNode Root;

        public Octree(Extents sceneExtents)
        {
            Root = null;
            _bbox = new Vector3[2];
            float xDiff = sceneExtents.D[0, 1] - sceneExtents.D[0, 0];
            float yDiff = sceneExtents.D[1, 1] - sceneExtents.D[1, 0];
            float zDiff = sceneExtents.D[2, 1] - sceneExtents.D[2, 0];
            float maxDiff = Mathf.Max(xDiff, Mathf.Max(yDiff, zDiff));
            Vector3 minPlusMax = new Vector3(
                sceneExtents.D[0, 0] + sceneExtents.D[0, 1],
                sceneExtents.D[1, 0] + sceneExtents.D[1, 1],
                sceneExtents.D[2, 0] + sceneExtents.D[2, 1]);
            _bbox[0] = (minPlusMax - new Vector3(maxDiff, maxDiff, maxDiff)) * 0.5f;
            _bbox[1] = (minPlusMax + new Vector3(maxDiff, maxDiff, maxDiff)) * 0.5f;
            Root = new OctreeNode();
        }


        public void Insert(Extents extents)
        {
            Insert(Root, extents, _bbox, 0);
        }

        public void Build()
        {
            Build(Root, _bbox);
        }


        public class QueueElement : FastPriorityQueueNode
        {
            public OctreeNode node;
            public float t;

            public QueueElement(OctreeNode n, float tn)
            {
                node = n;
                t = tn;
            }
        };


        private void Insert(OctreeNode node, Extents extents, Vector3[] bbox, int depth)
        {
            while (true)
            {
                if (node.IsLeaf)
                {
                    if (node.NodeExtentsList.Count == 0 || depth == 16)
                    {
                        node.NodeExtentsList.Add(extents);
                    }
                    else
                    {
                        node.IsLeaf = false;
                        // Re-insert extents held by this node
                        while (node.NodeExtentsList.Count > 0)
                        {
                            Insert(node, node.NodeExtentsList[node.NodeExtentsList.Count - 1], bbox, depth);
                            node.NodeExtentsList.RemoveAt(node.NodeExtentsList.Count - 1);
                        }

                        // Insert new extent
                        continue;
                    }
                }
                else
                {
                    // Need to compute in which child of the current node this extents should
                    // be inserted into
                    Vector3 extentsCentroid = extents.Centroid();
                    Vector3 nodeCentroid = (bbox[0] + bbox[1]) * 0.5f;
                    Vector3[] childBBox = new Vector3[2];
                    int childIndex = 0;
                    // x-axis
                    if (extentsCentroid.x > nodeCentroid.x)
                    {
                        childIndex = 4;
                        childBBox[0].x = nodeCentroid.x;
                        childBBox[1].x = bbox[1].x;
                    }
                    else
                    {
                        childBBox[0].x = bbox[0].x;
                        childBBox[1].x = nodeCentroid.x;
                    }

                    // y-axis
                    if (extentsCentroid.y > nodeCentroid.y)
                    {
                        childIndex += 2;
                        childBBox[0].y = nodeCentroid.y;
                        childBBox[1].y = bbox[1].y;
                    }
                    else
                    {
                        childBBox[0].y = bbox[0].y;
                        childBBox[1].y = nodeCentroid.y;
                    }

                    // z-axis
                    if (extentsCentroid.z > nodeCentroid.z)
                    {
                        childIndex += 1;
                        childBBox[0].z = nodeCentroid.z;
                        childBBox[1].z = bbox[1].z;
                    }
                    else
                    {
                        childBBox[0].z = bbox[0].z;
                        childBBox[1].z = nodeCentroid.z;
                    }

                    // Create the child node if it doesn't exsit yet and then insert the extents in it
                    node.Child[childIndex] ??= new OctreeNode();
                    node = node.Child[childIndex];
                    bbox = childBBox;
                    depth = depth + 1;
                    continue;
                }

                break;
            }
        }

        private static void Build(OctreeNode node, Vector3[] bbox)
        {
            if (node.IsLeaf)
            {
                foreach (Extents e in node.NodeExtentsList)
                {
                    node.NodeExtents.ExtendBy(e);
                }
            }
            else
            {
                for (int i = 0; i < 8; ++i)
                {
                    if (node.Child[i] == null) continue;
                    Vector3[] childBBox = new Vector3[2];
                    Vector3 centroid = (bbox[0] + bbox[1]) * 0.5f;
                    // x-axis
                    childBBox[0].x = (i & 4) == 1 ? centroid.x : bbox[0].x;
                    childBBox[1].x = (i & 4) == 1 ? bbox[1].x : centroid.x;
                    // y-axis
                    childBBox[0].y = (i & 2) == 1 ? centroid.y : bbox[0].y;
                    childBBox[1].y = (i & 2) == 1 ? bbox[1].y : centroid.y;
                    // z-axis
                    childBBox[0].z = (i & 1) == 1 ? centroid.z : bbox[0].z;
                    childBBox[1].z = (i & 1) == 1 ? bbox[1].z : centroid.z;

                    // Inspect child
                    Build(node.Child[i], childBBox);

                    // Expand extents with extents of child
                    node.NodeExtents.ExtendBy(node.Child[i].NodeExtents);
                }
            }
        }
    }
}