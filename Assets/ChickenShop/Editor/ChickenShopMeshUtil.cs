using UnityEngine;

namespace ChickenShop.EditorTools
{
    /// <summary>
    /// 월드 크기 기준 UV 를 가진 박스 메시 생성 (바닥·벽용).
    /// 기본 Cube 는 크기에 관계없이 UV 가 0~1 이라 타일이 늘어나므로, uvMeters 마다 텍스처가 한 번 반복되도록 만든다.
    /// </summary>
    public static class ChickenShopMeshUtil
    {
        public static Mesh CreateBox(Vector3 size, float uvMeters, string name)
        {
            var h = size * 0.5f;
            var verts = new Vector3[24];
            var normals = new Vector3[24];
            var uvs = new Vector2[24];
            var tris = new int[36];
            // 면: +X, -X, +Y, -Y, +Z, -Z
            Vector3[] n = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            for (int f = 0; f < 6; f++)
            {
                Vector3 normal = n[f];
                Vector3 u = f < 2 ? Vector3.forward : Vector3.right;   // 면의 가로 축
                Vector3 v = f == 2 || f == 3 ? Vector3.forward : Vector3.up; // 면의 세로 축
                if (f == 0) u = Vector3.back;
                if (f == 1) u = Vector3.forward;
                if (f == 5) u = Vector3.left;
                if (f == 3) v = Vector3.back;
                for (int k = 0; k < 4; k++)
                {
                    float su = (k == 1 || k == 2) ? 1f : -1f;
                    float sv = (k >= 2) ? 1f : -1f;
                    Vector3 p = Vector3.Scale(normal, h) + Vector3.Scale(u * su, h) + Vector3.Scale(v * sv, h);
                    int idx = f * 4 + k;
                    verts[idx] = p;
                    normals[idx] = normal;
                    // 월드 미터 기준 UV
                    float pu = Vector3.Dot(p, u) + Mathf.Abs(Vector3.Dot(h, u));
                    float pv = Vector3.Dot(p, v) + Mathf.Abs(Vector3.Dot(h, v));
                    uvs[idx] = new Vector2(pu / uvMeters, pv / uvMeters);
                }
                int t = f * 6, b = f * 4;
                tris[t] = b; tris[t + 1] = b + 2; tris[t + 2] = b + 1;
                tris[t + 3] = b; tris[t + 4] = b + 3; tris[t + 5] = b + 2;
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            // 앞면 방향이 법선과 일치하도록 확인 후 필요하면 뒤집기
            FixWinding(mesh);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static void FixWinding(Mesh mesh)
        {
            var v = mesh.vertices;
            var nrm = mesh.normals;
            var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                Vector3 faceN = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(faceN, nrm[t[i]]) < 0f)
                {
                    int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp;
                }
            }
            mesh.triangles = t;
        }
    }
}
