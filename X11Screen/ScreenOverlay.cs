using EasyOverlay.X11Screen.Interop;
using Modules;
using UnityEngine;
using Valve.VR;

namespace EasyOverlay.X11Screen
{
    public class ScreenOverlay : GrabbableOverlay
    {
        private XScreenCapture cap;
        
        [SerializeField]
        public int screen;
        
        private VROverlayIntersectionMaskPrimitive_t mask;
        private uint maskSize;

        protected override void LateUpdate()
        {
            base.LateUpdate();
            cap?.Tick();
        }
        
        protected override void OnEnable()
        {
            cap = new XScreenCapture(screen);
            texture = cap.texture;
            
            SetDeadzone(new Vector2Int(0, texture.width - texture.height)); 

            base.OnEnable();

            // TODO: vertical screens?
            // mask = new VROverlayIntersectionMaskPrimitive_t
            // {
            //     m_nPrimitiveType = EVROverlayIntersectionMaskPrimitiveType.OverlayIntersectionPrimitiveType_Rectangle,
            //     m_Primitive = new VROverlayIntersectionMaskPrimitive_Data_t
            //     {
            //         m_Rectangle = new IntersectionMaskRectangle_t
            //         {
            //             m_flHeight = cap.size.y,
            //             m_flWidth = cap.size.x,
            //             m_flTopLeftX = 0,
            //             m_flTopLeftY = deadPixelsY
            //         }
            //     }
            // };
            //
            // maskSize = sizeof(EVROverlayIntersectionMaskPrimitiveType) + 4 * sizeof(float);

            if (Application.isEditor)
                GenerateMesh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            cap?.Dispose();
            texture = null;
            cap = null;
        }

        protected override void OnMove(PointerHit pointer, bool primary)
        {
            if (primary) 
                cap?.MoveMouse(pointer.texUv);
        }

        protected override void OnLeft(TrackedDevice device, bool primary)
        {
        }

        protected override void OnPressed(PointerHit pointer)
        {
            SendMouse(pointer, true);
        }

        protected override void OnReleased(PointerHit pointer)
        {
            SendMouse(pointer, false);
        }

        private void SendMouse(PointerHit pointer, bool pressed)
        {
            var click = pointer.modifier switch
            {
                PointerModifier.RightClick => XcbMouseButton.Right,
                PointerModifier.MiddleClick => XcbMouseButton.Middle,
                _ => XcbMouseButton.Left
            };

            cap?.SendMouse(pointer.texUv, click, pressed, 0); // TODO keyboard modifiers
        }

        protected override void OnScroll(PointerHit pointer, float value)
        {
            
        }

        public override bool Render()
        {
            if (!base.Render()) 
                return false;

            var mouse = cap.GetMousePosition();
            
            if (mouse.x >= 0 && mouse.x < texture.width
                             && mouse.y >= 0 && mouse.y < texture.height)
            {
                var t = new HmdVector2_t
                {
                    v0 = 1,
                    v1 = 1
                };
                overlay.SetOverlayMouseScale(handle, ref t);

                t = new HmdVector2_t
                {
                    v0 = mouse.x / (float) texture.width,
                    v1 = 1 - (mouse.y / (float) texture.width + deadZone.y)
                };

                overlay.SetOverlayCursorPositionOverride(handle, ref t);
            }
            else
            {
                var t = new HmdVector2_t
                {
                    v0 = 0,
                    v1 = 0
                };
                overlay.SetOverlayMouseScale(handle, ref t);
            }
            
            UploadTexture();
            overlay.SetOverlayIntersectionMask(handle, ref mask, 1, maskSize);
            
            return true;
        }

        private void GenerateMesh()
        {
            // Number of horizontal vertices
            const int columns = 2;
            
            const int totalVerts = columns * 2;
            const int quads = columns - 1;
            const float colStep = 1f / quads;

            if (!gameObject.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = new Mesh();
            }

            var mesh = meshFilter.sharedMesh;
            mesh.Clear();

            if (!gameObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.material = new Material(Shader.Find("Unlit/Texture"));
                meshRenderer.material.mainTexture = texture;
            }
            
            var mainTex = meshRenderer.material.mainTexture;
            if (mainTex == null)
            {
                Debug.Log("Cannot create screen -- MainTex not set!");
                return;
            }
        
            var scale = transform.localScale;
            var size = new Vector2(mainTex.width * scale.x * 0.001f, mainTex.height * scale.y * 0.001f);
        
            var verts = new Vector3[totalVerts];
            var uv = new Vector2[totalVerts];
            var normals = new Vector3[totalVerts];
        
            var i = 0;
            for (var u = 0f; u <= 1f; u+=colStep)
            for (var v = 0f; v <= 1f; v+=1f)
            {
                var x = (-0.5f + u) * size.x;
                var y = (-0.5f + v) * size.y;
                var z = 0; // x * Mathf.Sin(x / radius);
                verts[i] = new Vector3(x, y, -z);
                uv[i] = new Vector2(1f-u, v);
                normals[i++] = Vector3.forward;
            }
        
            i = 0;
            
            var tris = new int[quads * 6];
            for (var q = 0; q < quads; q++)
            {
                var v = q * 2;
                // ◤
                tris[i++] = v;
                tris[i++] = v + 1;
                tris[i++] = v + 2;
                // ◢
                tris[i++] = v + 2;
                tris[i++] = v + 1;
                tris[i++] = v + 3;
            }
        
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.normals = normals;
            mesh.triangles = tris;
            
            mesh.RecalculateBounds();
        }
    }
}