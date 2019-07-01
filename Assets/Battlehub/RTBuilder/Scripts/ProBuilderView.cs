﻿using Battlehub.ProBuilderIntegration;
using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.RTHandles;
using Battlehub.UIControls;
using Battlehub.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Battlehub.RTBuilder
{
    public interface IProBuilderCmd
    {
        object Arg
        {
            get;
            set;
        }
        object Run();
        bool Validate();
    }
   
    public class ProBuilderView : RuntimeWindow
    {
        private class ProBuilderCmd : IProBuilderCmd
        {
            private Func<object, object> m_func;
            private Func<bool> m_validate;
            public string Text;
            public bool CanDrag;

            public ProBuilderCmd Parent;
            public List<ProBuilderCmd> Children;
            public bool HasChildren
            {
                get { return Children != null && Children.Count > 0; }
            }
            public bool HasParent
            {
                get { return Parent != null; }
            }

            public object Arg
            {
                get;
                set;
            }

            public ProBuilderCmd(string text, Func<object, object> func, bool canDrag = false) : this(text, func, () => true, canDrag)
            {
            }

            public ProBuilderCmd(string text, Func<object, object> func, Func<bool> validate = null, bool canDrag = false)
            {
                Text = text;
                m_func = func;
                m_validate = validate;
                if(m_validate == null)
                {
                    m_validate = () => true;
                }
                CanDrag = canDrag;
            }

            public ProBuilderCmd(string text, Action action, Func<bool> validate = null, bool canDrag = false)
            {
                Text = text;
                m_func = args => { action(); return null; };
                m_validate = validate;
                if(m_validate == null)
                {
                    m_validate = () => true;
                }
                CanDrag = canDrag;
            }

            public object Run()
            {
                return m_func(Arg);
            }

            public bool Validate()
            {
                return m_validate();
            }
        }


        [SerializeField]
        private VirtualizingTreeView m_commandsList = null;

        [SerializeField]
        private bool m_useSceneViewToolbar = true;
        [SerializeField]
        private ProBuilderToolbar m_sceneViewToolbarPrefab = null;
        [SerializeField]
        private bool m_useToolbar = false;
        [SerializeField]
        private ProBuilderToolbar m_toolbar = null;

        private ProBuilderCmd[] m_commands;
        private GameObject m_proBuilderToolGO;
        private IProBuilderTool m_proBuilderTool;

        
        private bool m_isProBuilderMeshSelected = false;
        private bool m_isNonProBuilderMeshSelected = false;
        
        private IWindowManager m_wm;
        
        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.Custom;
            base.AwakeOverride();

            m_wm = IOC.Resolve<IWindowManager>();
            m_wm.WindowCreated += OnWindowCreated;
            m_wm.WindowDestroyed += OnWindowDestroyed;

            m_proBuilderToolGO = new GameObject("ProBuilderTool");
            m_proBuilderToolGO.transform.SetParent(Editor.Root, false);
            m_proBuilderTool = m_proBuilderToolGO.AddComponent<ProBuilderTool>();
            m_proBuilderToolGO.AddComponent<MaterialPaletteManager>();
            m_proBuilderTool.ModeChanged += OnProBuilderToolModeChanged;
            m_proBuilderTool.SelectionChanged += OnProBuilderToolSelectionChanged;
            
            
            CreateToolbar();
            m_toolbar.gameObject.SetActive(m_useToolbar);

            Editor.Undo.Store();

            Editor.Selection.SelectionChanged += OnSelectionChanged;

            m_commandsList.ItemClick += OnItemClick;
            m_commandsList.ItemDataBinding += OnItemDataBinding;
            m_commandsList.ItemExpanding += OnItemExpanding;
            m_commandsList.ItemBeginDrag += OnItemBeginDrag;
            m_commandsList.ItemDrop += OnItemDrop;
            m_commandsList.ItemDragEnter += OnItemDragEnter;
            m_commandsList.ItemDragExit += OnItemDragExit;
            m_commandsList.ItemEndDrag += OnItemEndDrag;

            m_commandsList.CanEdit = false;
            m_commandsList.CanReorder = false;
            m_commandsList.CanReparent = false;
            m_commandsList.CanSelectAll = false;
            m_commandsList.CanUnselectAll = true;
            m_commandsList.CanRemove = false;
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            if (m_wm != null)
            {
                m_wm.WindowCreated -= OnWindowCreated;
                m_wm.WindowDestroyed -= OnWindowDestroyed;
                DestroyToolbar();
            }

            if (m_proBuilderToolGO != null)
            {
                Destroy(m_proBuilderToolGO);
            }

            Editor.Undo.Restore();

            if (Editor != null)
            {
                Editor.Selection.SelectionChanged -= OnSelectionChanged;
            }

            if (m_commandsList != null)
            {
                m_commandsList.ItemClick -= OnItemClick;
                m_commandsList.ItemDataBinding -= OnItemDataBinding;
                m_commandsList.ItemExpanding -= OnItemExpanding;
                m_commandsList.ItemBeginDrag -= OnItemBeginDrag;
                m_commandsList.ItemDrop -= OnItemDrop;
                m_commandsList.ItemDragEnter -= OnItemDragEnter;
                m_commandsList.ItemDragExit -= OnItemDragExit;
                m_commandsList.ItemEndDrag -= OnItemEndDrag;
            }

            if (m_proBuilderTool != null)
            {
                m_proBuilderTool.ModeChanged -= OnProBuilderToolModeChanged;
                m_proBuilderTool.SelectionChanged -= OnProBuilderToolSelectionChanged;
            }
        }

        protected virtual void Start()
        {
            m_commands = GetCommands().ToArray();
            m_commandsList.Items = m_commands;
        }

        private void OnProBuilderToolModeChanged(ProBuilderToolMode mode)
        {
            m_commands = GetCommands().ToArray();
            m_commandsList.Items = m_commands;
        }

        private List<ProBuilderCmd> GetCommands()
        {
            switch (m_proBuilderTool.Mode)
            {
                case ProBuilderToolMode.Object:
                    return GetObjectCommands();
                case ProBuilderToolMode.Face:
                    return GetFaceCommands();
                case ProBuilderToolMode.Edge:
                    return GetEdgeCommands();
                case ProBuilderToolMode.Vertex:
                    return GetVertexCommands();
            }
            return new List<ProBuilderCmd>();
        }

        private List<ProBuilderCmd> GetObjectCommands()
        {
            List<ProBuilderCmd> commands = GetCommonCommands();
            commands.Add(new ProBuilderCmd("ProBuilderize", OnProBuilderize, CanProBuilderize));
            commands.Add(new ProBuilderCmd("Subdivide", () => m_proBuilderTool.Subdivide(), () => m_isProBuilderMeshSelected));
            

            return commands;
        }

        private List<ProBuilderCmd> GetFaceCommands()
        {
            List<ProBuilderCmd> commands = GetCommonCommands();
            commands.Add(new ProBuilderCmd("Extrude Face", OnExtrudeFace, CanExtrudeFace));
            commands.Add(new ProBuilderCmd("Delete Face", OnDeleteFace, CanDeleteFace));
            return commands;
        }

        private List<ProBuilderCmd> GetEdgeCommands()
        {
            List<ProBuilderCmd> commands = GetCommonCommands();
            commands.Add(new ProBuilderCmd("Find Holes", () => m_proBuilderTool.SelectHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            commands.Add(new ProBuilderCmd("Fill Holes", () => m_proBuilderTool.FillHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            return commands;
        }

        private List<ProBuilderCmd> GetVertexCommands()
        {
            List<ProBuilderCmd> commands = GetCommonCommands();
            commands.Add(new ProBuilderCmd("Find Holes", () => m_proBuilderTool.SelectHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            commands.Add(new ProBuilderCmd("Fill Holes", () => m_proBuilderTool.FillHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            return commands;
        }

        private List<ProBuilderCmd> GetCommonCommands()
        {
            List<ProBuilderCmd> commands = new List<ProBuilderCmd>();
            ProBuilderCmd newShapeCmd = new ProBuilderCmd("New Shape", OnNewShape, true) { Arg = PBShapeType.Cube };
            newShapeCmd.Children = new List<ProBuilderCmd>
            {
                new ProBuilderCmd("Arch", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Arch },
                new ProBuilderCmd("Cone", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Cone },
                new ProBuilderCmd("Cube", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Cube },
                new ProBuilderCmd("Curved Stair", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.CurvedStair },
                new ProBuilderCmd("Cylinder", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Cylinder },
                new ProBuilderCmd("Door", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Door },
                new ProBuilderCmd("Pipe", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Pipe },
                new ProBuilderCmd("Plane", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Plane },
                new ProBuilderCmd("Prism", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Prism },
                new ProBuilderCmd("Sphere", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Sphere },
                new ProBuilderCmd("Sprite", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Sprite },
                new ProBuilderCmd("Stair", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Stair },
                new ProBuilderCmd("Torus", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Torus },
            };

            commands.Add(newShapeCmd);
            commands.Add(new ProBuilderCmd("New Poly Shape", OnNewPolyShape, true));
            commands.Add(new ProBuilderCmd("Edit Materials", OnEditMaterials));
            commands.Add(new ProBuilderCmd("Edit UV", OnEditUV));
            return commands;
        }

        private void OnSelectionChanged(UnityEngine.Object[] unselectedObjects)
        {
            GameObject[] selected = Editor.Selection.gameObjects;
            if (selected != null && selected.Length > 0)
            {
                m_isProBuilderMeshSelected = selected.Where(go => go.GetComponent<PBMesh>() != null).Any();
                m_isNonProBuilderMeshSelected = selected.Where(go => go.GetComponent<PBMesh>() == null).Any();
            }
            else
            {
                m_isProBuilderMeshSelected = false;
                m_isNonProBuilderMeshSelected = false;
            }

            int index = m_commandsList.VisibleItemIndex;
            int count = m_commandsList.VisibleItemsCount;
            for (int i = 0; i < count; ++i)
            {
                m_commandsList.DataBindItem(m_commands[i]);
            }
        }


        private void OnProBuilderToolSelectionChanged()
        {
            GameObject[] selected = Editor.Selection.gameObjects;
            if (selected != null && selected.Length > 0)
            {
                m_isProBuilderMeshSelected = selected.Where(go => go.GetComponent<PBMesh>() != null).Any();
                m_isNonProBuilderMeshSelected = selected.Where(go => go.GetComponent<PBMesh>() == null).Any();
            }
            else
            {
                m_isProBuilderMeshSelected = false;
                m_isNonProBuilderMeshSelected = false;
            }

            m_commandsList.DataBindVisible();
        }


        private void OnItemDataBinding(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>();
            ProBuilderCmd cmd = (ProBuilderCmd)e.Item;
            text.text = cmd.Text;

            bool isValid = cmd.Validate();
            Color color = text.color;
            color.a = isValid ? 1 : 0.5f;
            text.color = color;
          
            e.CanDrag = cmd.CanDrag;
            e.HasChildren = cmd.HasChildren;
        }

        private void OnItemExpanding(object sender, VirtualizingItemExpandingArgs e)
        {
            ProBuilderCmd cmd = (ProBuilderCmd)e.Item;
            e.Children = cmd.Children;
        }

        private void OnItemClick(object sender, ItemArgs e)
        {
            ProBuilderCmd cmd = (ProBuilderCmd)e.Items[0];
            if(cmd.Validate())
            {
                cmd.Run();
            }
        }

        private object OnNewShape(object arg)
        {
            GameObject go = PBShapeGenerator.CreateShape((PBShapeType)arg);
            go.AddComponent<PBMesh>();

            Renderer renderer = go.GetComponent<Renderer>();
            if(renderer != null && renderer.sharedMaterials.Length == 1 && renderer.sharedMaterials[0] == PBBuiltinMaterials.DefaultMaterial)
            {
                IMaterialPaletteManager paletteManager = IOC.Resolve<IMaterialPaletteManager>();
                if(paletteManager.Palette.Materials.Count > 0)
                {
                    renderer.sharedMaterial = paletteManager.Palette.Materials[0];
                }
            }

            IRuntimeEditor rte = IOC.Resolve<IRuntimeEditor>();
            RuntimeWindow scene = rte.GetWindow(RuntimeWindowType.Scene);
            Vector3 position;
            Quaternion rotation;
            GetPositionAndRotation(scene, out position, out rotation);

            ExposeToEditor exposeToEditor = go.AddComponent<ExposeToEditor>();
            go.transform.position = position + rotation * Vector3.up * exposeToEditor.Bounds.extents.y;
            go.transform.rotation = rotation;
         
            Editor.Undo.BeginRecord();
            Editor.Selection.activeGameObject = go;
            Editor.Undo.RegisterCreatedObjects(new[] { exposeToEditor });
            Editor.Undo.EndRecord();

            return go;
        }

        private object OnNewPolyShape(object arg)
        {
            GameObject go = (GameObject)OnNewShape(PBShapeType.Cube);
            go.name = "Poly Shape";
            PBMesh pbMesh = go.GetComponent<PBMesh>();
            pbMesh.Clear();
            
            PBPolyShape polyShape = go.AddComponent<PBPolyShape>();
            polyShape.AddVertex(Vector3.zero);
            return go;
        }

        private void GetPositionAndRotation(RuntimeWindow window, out Vector3 position, out Quaternion rotation)
        {
            Ray ray = window != null ? 
                new Ray(window.Camera.transform.position, window.Camera.transform.forward) : 
                new Ray(Vector3.up * 100000, Vector3.down);

            RaycastHit[] hits = Physics.RaycastAll(ray);
            for(int i = 0; i < hits.Length; ++i)
            {
                RaycastHit hit = hits[i];
                if (hit.collider is TerrainCollider)
                {
                    position = hit.point;
                    rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    return;
                }
            }

            Vector3 up = Vector3.up;
            Vector3 pivot = Vector3.zero;
            if (window != null)
            {
                IScenePivot scenePivot = window.IOCContainer.Resolve<IScenePivot>();
                if (Mathf.Abs(Vector3.Dot(window.Camera.transform.up, Vector3.up)) > Mathf.Cos(Mathf.Deg2Rad))
                {
                    up = Vector3.Cross(window.Camera.transform.right, Vector3.up);
                }

                pivot = scenePivot.SecondaryPivot;
            }

            Plane dragPlane = new Plane(up, pivot);
            rotation = Quaternion.identity;
            if (!GetPointOnDragPlane(ray, dragPlane, out position))
            {
                position = window.Camera.transform.position + window.Camera.transform.forward * 10.0f;
            }
        }

        private bool GetPointOnDragPlane(Ray ray, Plane dragPlane, out Vector3 point)
        {
            float distance;
            if (dragPlane.Raycast(ray, out distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }
            point = Vector3.zero;
            return false;
        }

        private void OnEditMaterials()
        {
            m_wm.CreateWindow("MaterialPalette", false, UIControls.DockPanels.RegionSplitType.Left, 0.2f);
        }

        private void OnEditUV()
        {
            m_wm.CreateWindow("UVEditor", false, UIControls.DockPanels.RegionSplitType.Left, 0.2f);
        }

        private bool CanProBuilderize()
        {
            return m_isNonProBuilderMeshSelected;
        }

        private object OnProBuilderize(object arg)
        {
            GameObject[] gameObjects = Editor.Selection.gameObjects;
            if(gameObjects == null)
            {
                return null;
            }

            for(int i = 0; i < gameObjects.Length; ++i)
            {
                MeshFilter[] filters = gameObjects[i].GetComponentsInChildren<MeshFilter>();
                for(int j = 0; j < filters.Length; ++j)
                {
                    PBMesh.ProBuilderize(filters[j].gameObject);
                }
            }
            return null;
        }

        private bool CanExtrudeFace()
        {
            return m_proBuilderTool.Mode == ProBuilderToolMode.Face && m_proBuilderTool.HasSelection;
        }

        private object OnExtrudeFace(object arg)
        {
            m_proBuilderTool.Extrude(0.01f);
            return null;
        }

        private bool CanDeleteFace()
        {
            return m_proBuilderTool.Mode == ProBuilderToolMode.Face && m_proBuilderTool.HasSelection;
        }

        private void OnDeleteFace()
        {
            m_proBuilderTool.DeleteFaces();
        }

        private void OnItemBeginDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseBeginDrag(this, e.Items, e.PointerEventData);
        }

        private void OnItemDragEnter(object sender, ItemDropCancelArgs e)
        {
            Editor.DragDrop.SetCursor(KnownCursor.DropNowAllowed);
            e.Cancel = true;
        }

        private void OnItemDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseDrag(e.PointerEventData);
        }

        private void OnItemDragExit(object sender, EventArgs e)
        {
            Editor.DragDrop.SetCursor(KnownCursor.DropNowAllowed);
        }

        private void OnItemDrop(object sender, ItemDropArgs e)
        {
            Editor.DragDrop.RaiseDrop(e.PointerEventData);
        }

        private void OnItemEndDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseDrop(e.PointerEventData);
        }

        private void CreateToolbar()
        {
            Transform[] scenes = m_wm.GetWindows(RuntimeWindowType.Scene.ToString());
            for(int i = 0; i < scenes.Length; ++i)
            {
                RuntimeWindow window = scenes[i].GetComponent<RuntimeWindow>();
                CreateToolbar(scenes[i], window);
            }
        }

        private void DestroyToolbar()
        {
            Transform[] scenes = m_wm.GetWindows(RuntimeWindowType.Scene.ToString());
            for(int i = 0; i < scenes.Length; ++i)
            {
                RuntimeWindow window = scenes[i].GetComponent<RuntimeWindow>();
                DestroyToolbar(scenes[i], window);
            }
        }

        private void OnWindowCreated(Transform windowTransform)
        {
            RuntimeWindow window = windowTransform.GetComponent<RuntimeWindow>();
            CreateToolbar(windowTransform, window);
        }

        private void CreateToolbar(Transform windowTransform, RuntimeWindow window)
        {
            if(m_useSceneViewToolbar)
            {
                if (window != null && window.WindowType == RuntimeWindowType.Scene)
                {
                    if (m_sceneViewToolbarPrefab != null)
                    {
                        RectTransform rt = (RectTransform)Instantiate(m_sceneViewToolbarPrefab, windowTransform, false).transform;
                        rt.Stretch();
                    }
                }
            }
        }

        private void OnWindowDestroyed(Transform windowTransform)
        {
            if (m_useSceneViewToolbar)
            {
                RuntimeWindow window = windowTransform.GetComponent<RuntimeWindow>();
                DestroyToolbar(windowTransform, window);
            }
        }

        private void DestroyToolbar(Transform windowTransform, RuntimeWindow window)
        {
            if (window != null && window.WindowType == RuntimeWindowType.Scene)
            {
                if (m_sceneViewToolbarPrefab != null)
                {
                    ProBuilderToolbar toolbar = windowTransform.GetComponentInChildren<ProBuilderToolbar>();
                    if (toolbar != null)
                    {
                        Destroy(toolbar.gameObject);
                    }
                }
            }
        }

    }
}


