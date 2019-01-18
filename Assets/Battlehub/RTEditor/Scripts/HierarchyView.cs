﻿using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

using Battlehub.UIControls;
using Battlehub.RTCommon;
using Battlehub.RTSaveLoad2.Interface;
using Battlehub.Utils;

namespace Battlehub.RTEditor
{
    public class HierarchyView : RuntimeWindow
    {
        public GameObject TreeViewPrefab;
        private VirtualizingTreeView m_treeView;
        public Color DisabledItemColor = new Color(0.5f, 0.5f, 0.5f);
        public Color EnabledItemColor = new Color(0.2f, 0.2f, 0.2f);
        public UnityEvent ItemDoubleClick;
        private bool m_lockSelection;
        private bool m_isSpawningPrefab;
        private bool m_isStarted;

        private IProject m_project;
        private IRuntimeEditor m_editor;

        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.Hierarchy;
            base.AwakeOverride();
            if (!TreeViewPrefab)
            {
                Debug.LogError("Set TreeViewPrefab field");
                return;
            }
            
            m_project = IOC.Resolve<IProject>();
            m_editor = IOC.Resolve<IRuntimeEditor>();

            m_treeView = Instantiate(TreeViewPrefab, transform).GetComponent<VirtualizingTreeView>();
            m_treeView.name = "HierarchyTreeView";
            m_treeView.CanSelectAll = false;
            m_treeView.SelectOnPointerUp = true;

            RectTransform rt = (RectTransform)m_treeView.transform;
            rt.Stretch();
                        
            m_treeView.ItemDataBinding += OnItemDataBinding;
            m_treeView.SelectionChanged += OnSelectionChanged;
            m_treeView.ItemsRemoved += OnItemsRemoved;
            m_treeView.ItemExpanding += OnItemExpanding;
            m_treeView.ItemBeginDrag += OnItemBeginDrag;
            m_treeView.ItemBeginDrop += OnItemBeginDrop;
            m_treeView.ItemDrag += OnItemDrag;
            m_treeView.ItemDrop += OnItemDrop;
            m_treeView.ItemEndDrag += OnItemEndDrag;
            m_treeView.ItemDragEnter += OnItemDragEnter;
            m_treeView.ItemDragExit += OnItemDragExit;
            m_treeView.ItemDoubleClick += OnItemDoubleClicked;
            m_treeView.ItemBeginEdit += OnItemBeginEdit;
            m_treeView.ItemEndEdit += OnItemEndEdit;  
            
            if(!GetComponent<HierarchyViewInput>())
            {
                gameObject.AddComponent<HierarchyViewInput>();
            }
        }

        private void Start()
        {
            m_isStarted = true;
        }

        private void OnEnable()
        {
            if(m_editor != null)
            {
                m_editor.SceneLoading += OnSceneLoading;
                m_editor.SceneLoaded += OnSceneLoaded;
            }

            EnableHierarchy();    
        }

        private void OnDisable()
        {
            if (m_editor != null)
            {
                m_editor.SceneLoading -= OnSceneLoading;
                m_editor.SceneLoaded -= OnSceneLoaded;
            }

            DisableHierarchy();
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            if (!m_treeView)
            {
                return;
            }
            m_treeView.ItemDataBinding -= OnItemDataBinding;
            m_treeView.SelectionChanged -= OnSelectionChanged;
            m_treeView.ItemsRemoved -= OnItemsRemoved;
            m_treeView.ItemExpanding -= OnItemExpanding;
            m_treeView.ItemBeginDrag -= OnItemBeginDrag;
            m_treeView.ItemBeginDrop -= OnItemBeginDrop;
            m_treeView.ItemDrag -= OnItemDrag;
            m_treeView.ItemDrop -= OnItemDrop;
            m_treeView.ItemEndDrag -= OnItemEndDrag;
            m_treeView.ItemDragEnter -= OnItemDragEnter;
            m_treeView.ItemDragExit -= OnItemDragExit;
            m_treeView.ItemDoubleClick -= OnItemDoubleClicked;
            m_treeView.ItemBeginEdit -= OnItemBeginEdit;
            m_treeView.ItemEndEdit -= OnItemEndEdit;     
        }

        private void OnApplicationQuit()
        {
            if(Editor != null)
            {
                Editor.Object.Awaked -= OnObjectAwaked;
                Editor.Object.Started -= OnObjectStarted;
                Editor.Object.Enabled -= OnObjectEnabled;
                Editor.Object.Disabled -= OnObjectDisabled;
                Editor.Object.Destroying -= OnObjectDestroying;
                Editor.Object.Destroyed -= OnObjectDestroyed;
                Editor.Object.MarkAsDestroyedChanged -= OnObjectMarkAsDestroyedChanged;
                Editor.Object.ParentChanged -= OnParentChanged;
                Editor.Object.NameChanged -= OnNameChanged;
                Editor.PlaymodeStateChanged -= OnPlaymodeStateChanged;
            }

            if (m_editor != null)
            {
                m_editor.SceneLoading -= OnSceneLoading;
                m_editor.SceneLoaded -= OnSceneLoaded;
            }
        }

        public void SelectAll()
        {
            m_treeView.SelectedItems = m_treeView.Items;
        }

        
        private void EnableHierarchy()
        {
            BindGameObjects();
            m_lockSelection = true;
            m_treeView.SelectedItems = Editor.Selection.gameObjects != null ? Editor.Selection.gameObjects.Select(g => g.GetComponent<ExposeToEditor>()).Where(e => e != null).ToArray() : null;
            m_lockSelection = false;

            Editor.Selection.SelectionChanged += OnRuntimeSelectionChanged;

            Editor.Object.Awaked += OnObjectAwaked;
            Editor.Object.Started += OnObjectStarted;
            Editor.Object.Enabled += OnObjectEnabled;
            Editor.Object.Disabled += OnObjectDisabled;
            Editor.Object.Destroying += OnObjectDestroying;
            Editor.Object.Destroyed += OnObjectDestroyed;
            Editor.Object.MarkAsDestroyedChanging += OnObjectMarkAsDestoryedChanging;
            Editor.Object.MarkAsDestroyedChanged += OnObjectMarkAsDestroyedChanged;
            Editor.Object.ParentChanged += OnParentChanged;
            Editor.Object.NameChanged += OnNameChanged;

            Editor.PlaymodeStateChanged += OnPlaymodeStateChanged;
        }

        private void DisableHierarchy()
        {
            if(Editor != null)
            {
                Editor.Selection.SelectionChanged -= OnRuntimeSelectionChanged;

                Editor.Object.Awaked -= OnObjectAwaked;
                Editor.Object.Started -= OnObjectStarted;
                Editor.Object.Enabled -= OnObjectEnabled;
                Editor.Object.Disabled -= OnObjectDisabled;
                Editor.Object.Destroying -= OnObjectDestroying;
                Editor.Object.Destroyed -= OnObjectDestroyed;
                Editor.Object.MarkAsDestroyedChanging -= OnObjectMarkAsDestoryedChanging;
                Editor.Object.MarkAsDestroyedChanged -= OnObjectMarkAsDestroyedChanged;
                Editor.Object.ParentChanged -= OnParentChanged;
                Editor.Object.NameChanged -= OnNameChanged;

                Editor.PlaymodeStateChanged -= OnPlaymodeStateChanged;
            }
        }

        private void OnSceneLoading()
        {
            DisableHierarchy();
        }

        private void OnSceneLoaded()
        {
            EnableHierarchy();
        }

        private void BindGameObjects()
        {
            bool useCache = Editor.IsPlaying;
            IEnumerable<ExposeToEditor> objects = Editor.Object.Get(true, useCache);

            if(objects.Any())
            {
                Transform commonParent = objects.First().transform.parent;
                foreach(ExposeToEditor obj in objects)
                {
                    if(obj.transform.parent != commonParent)
                    {
                        Debug.LogWarning("ExposeToEditor objects have different parents, hierarchy may not work correctly.");
                        break;
                    }
                }
            }
            m_treeView.Items = objects.OrderBy(g => g.transform.GetSiblingIndex());
        }

        private void OnPlaymodeStateChanged()
        {
            BindGameObjects();
        }

        private void OnItemExpanding(object sender, VirtualizingItemExpandingArgs e)
        {
            ExposeToEditor exposeToEditor = (ExposeToEditor)e.Item;
            
            if (exposeToEditor.ChildCount > 0)
            {
                e.Children = exposeToEditor.GetChildren().Where(obj => !obj.MarkAsDestroyed);

                //This line is required to syncronize selection, runtime selection and treeview selection
                OnTreeViewSelectionChanged(m_treeView.SelectedItems, m_treeView.SelectedItems);
            }
            else
            {
                e.Children = new ExposeToEditor[0];
            }
        }

        private void OnRuntimeSelectionChanged(Object[] unselected)
        {
            if (m_lockSelection)
            {
                return;
            }
            m_lockSelection = true;

            if(Editor.Selection.gameObjects == null)
            {
                m_treeView.SelectedItems = new ExposeToEditor[0];
            }
            else
            {
                m_treeView.SelectedItems = Editor.Selection.gameObjects.Select(g => g.GetComponent<ExposeToEditor>()).Where(e => e != null).ToArray();
            }
            
            m_lockSelection = false;
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs e)
        {
            OnTreeViewSelectionChanged(e.OldItems, e.NewItems);
        }

        private void OnTreeViewSelectionChanged(IEnumerable oldItems, IEnumerable newItems)
        {
            if (m_lockSelection)
            {
                return;
            }

            m_lockSelection = true;

            if (newItems == null)
            {
                newItems = new ExposeToEditor[0];
            }
            ExposeToEditor[] selectableObjects = newItems.OfType<ExposeToEditor>().Where(o => o.CanSelect).ToArray();
            Editor.Selection.objects = selectableObjects.Select(o => o.gameObject).ToArray();

            //sync with RunitimeSelectiom.objects because of OnBeforeSelectionChanged event
            m_treeView.SelectedItems = selectableObjects;

            m_lockSelection = false;
        }

        private void OnItemsRemoved(object sender, ItemsRemovedArgs e)
        {
            if(Editor.ActiveWindow == this)
            {
                IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
                editor.Delete(e.Items.OfType<ExposeToEditor>().Select(exposed => exposed.gameObject).ToArray());
            }
            //Removal handled in RuntimeEditor class
        }

        private void OnItemDataBinding(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            ExposeToEditor dataItem = (ExposeToEditor)e.Item;
            if (dataItem != null)
            {
                Text text = e.ItemPresenter.GetComponentInChildren<Text>(true);
                text.text = dataItem.name;
                if (dataItem.gameObject.activeInHierarchy)
                {
                    text.color = EnabledItemColor;
                }
                else
                {
                    text.color = DisabledItemColor;
                }

                e.HasChildren = dataItem.GetComponent<ExposeToEditor>().ChildCount > 0;
            }
        }

        private void OnItemDoubleClicked(object sender, ItemArgs e)
        {
            ExposeToEditor exposeToEditor = (ExposeToEditor)e.Items[0];
            if (exposeToEditor.CanSelect)
            {
                Editor.Selection.activeObject = exposeToEditor.gameObject;
                ItemDoubleClick.Invoke();
            }
        }

        private void OnItemBeginEdit(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            ExposeToEditor dataItem = (ExposeToEditor)e.Item;
            if (dataItem != null)
            {
                InputField inputField = e.EditorPresenter.GetComponentInChildren<InputField>(true);
                inputField.text = dataItem.name;
                inputField.ActivateInputField();
                inputField.Select();
                LayoutElement layout = inputField.GetComponent<LayoutElement>();

                Text text = e.ItemPresenter.GetComponentInChildren<Text>(true);
                text.text = dataItem.name;

                RectTransform rt = text.GetComponent<RectTransform>();
                layout.preferredWidth = rt.rect.width;
            }
        }

        private void OnItemEndEdit(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            ExposeToEditor dataItem = (ExposeToEditor)e.Item ;
            if (dataItem != null)
            {
                
                InputField inputField = e.EditorPresenter.GetComponentInChildren<InputField>(true);
                if (!string.IsNullOrEmpty(inputField.text))
                {
                    dataItem.name = inputField.text;
                    Text text = e.ItemPresenter.GetComponentInChildren<Text>(true);
                    text.text = dataItem.name;
                }
                else
                {
                    inputField.text = dataItem.name;
                }
            }

            //Following code is required to unfocus inputfield if focused and release InputManager
            if (EventSystem.current != null && !EventSystem.current.alreadySelecting)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void OnItemDragEnter(object sender, ItemDropCancelArgs e)
        {
            Editor.DragDrop.SetCursor(KnownCursor.DropAllowed);
        }

        private void OnItemDragExit(object sender, System.EventArgs e)
        {
            Editor.DragDrop.SetCursor(KnownCursor.DropNowAllowed);
        }

        private void OnItemBeginDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseBeginDrag(this, e.Items, e.PointerEventData);
        }

        private void OnItemDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseDrag(e.PointerEventData);
        }

        private void OnItemBeginDrop(object sender, ItemDropCancelArgs e)
        {
            if(e.IsExternal)
            {
                
            }
            else
            {
                Editor.Undo.BeginRecord();
                for (int i = 0; i < e.DragItems.Length; ++i)
                {
                    ExposeToEditor exposed = (ExposeToEditor)e.DragItems[i];
                    Transform dragT = exposed.transform;
                    Editor.Undo.RecordTransform(dragT, dragT.parent, dragT.GetSiblingIndex());
                    Editor.Undo.RecordObject(exposed, m_treeView.IndexOf(exposed), RestoreIndexFromUndoRecord);
                }
                Editor.Undo.EndRecord();
            }
        }

        private void OnItemDrop(object sender, ItemDropArgs e)
        {
            if (e.IsExternal)
            {
                
            }
            else
            {
                Editor.DragDrop.RaiseDrop(e.PointerEventData);

                Transform dropT = ((ExposeToEditor)e.DropTarget).transform;
                if (e.Action == ItemDropAction.SetLastChild)
                {
                    Editor.Undo.BeginRecord();
                    for (int i = 0; i < e.DragItems.Length; ++i)
                    {
                        ExposeToEditor exposed = (ExposeToEditor)e.DragItems[i];
                        Transform dragT = exposed.transform;
                        dragT.SetParent(dropT, true);
                        dragT.SetAsLastSibling();

                        Editor.Undo.RecordTransform(dragT, dropT, dragT.GetSiblingIndex());
                        Editor.Undo.RecordObject(exposed, m_treeView.IndexOf(exposed), RestoreIndexFromUndoRecord);

                    }
                    Editor.Undo.EndRecord();
                }
                else if (e.Action == ItemDropAction.SetNextSibling)
                {
                    Editor.Undo.BeginRecord();
                    for (int i = e.DragItems.Length - 1; i >= 0; --i)
                    {
                        ExposeToEditor exposed = (ExposeToEditor)e.DragItems[i];
                        Transform dragT = exposed.transform;
                        int dropTIndex = dropT.GetSiblingIndex();
                        if (dragT.parent != dropT.parent)
                        {
                            dragT.SetParent(dropT.parent, true);
                            dragT.SetSiblingIndex(dropTIndex + 1);
                        }
                        else
                        {
                            int dragTIndex = dragT.GetSiblingIndex();
                            if (dropTIndex < dragTIndex)
                            {
                                dragT.SetSiblingIndex(dropTIndex + 1);
                            }
                            else
                            {
                                dragT.SetSiblingIndex(dropTIndex);
                            }
                        }

                        Editor.Undo.RecordTransform(dragT, dropT.parent, dragT.GetSiblingIndex());
                        Editor.Undo.RecordObject(exposed, m_treeView.IndexOf(exposed), RestoreIndexFromUndoRecord);

                    }

                    Editor.Undo.EndRecord();
                }
                else if (e.Action == ItemDropAction.SetPrevSibling)
                {
                    Editor.Undo.BeginRecord();
                    for (int i = 0; i < e.DragItems.Length; ++i)
                    {
                        ExposeToEditor exposed = (ExposeToEditor)e.DragItems[i];
                        Transform dragT = exposed.transform;
                        if (dragT.parent != dropT.parent)
                        {
                            dragT.SetParent(dropT.parent, true);
                        }

                        int dropTIndex = dropT.GetSiblingIndex();
                        int dragTIndex = dragT.GetSiblingIndex();
                        if (dropTIndex > dragTIndex)
                        {
                            dragT.SetSiblingIndex(dropTIndex - 1);
                        }
                        else
                        {
                            dragT.SetSiblingIndex(dropTIndex);
                        }

                        Editor.Undo.RecordTransform(dragT, dropT.parent, dragT.GetSiblingIndex());
                        Editor.Undo.RecordObject(exposed, m_treeView.IndexOf(exposed), RestoreIndexFromUndoRecord);

                    }
                    Editor.Undo.EndRecord();
                }
            }
        }

        private bool RestoreIndexFromUndoRecord(Record record)
        {
            //int currentIndex = m_treeView.IndexOf(record.Target);

            int index = (int)record.State;
            //bool hasChanged = currentIndex != index;

            //if (hasChanged)
            {
                m_treeView.SetIndex(record.Target, index);
                m_treeView.UpdateIndent(record.Target);
            }
            return false;
        }

        private void OnItemEndDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseDrop(e.PointerEventData);
        }

        private void OnObjectAwaked(ExposeToEditor obj)
        {
            /*
            if (m_isSpawningPrefab && m_treeView.DropAction != ItemDropAction.None)
            {
                VirtualizingTreeViewItem treeViewItem = m_treeView.GetTreeViewItem(m_treeView.DropTarget);
                ExposeToEditor dropTarget = (ExposeToEditor)m_treeView.DropTarget;
                if (m_treeView.DropAction == ItemDropAction.SetLastChild)
                {
                    obj.Parent = dropTarget;
                    obj.transform.SetParent(dropTarget.transform);
                    if (m_treeView.IndexOf(obj) == -1)
                    {
                        m_treeView.AddChild(dropTarget, obj);
                    }
                    treeViewItem.CanExpand = true;
                    treeViewItem.IsExpanded = true;
                }
                else
                {
                    int index;
                    int siblingIndex;
                    if (m_treeView.DropAction == ItemDropAction.SetNextSibling)
                    {
                        index = m_treeView.IndexOf(dropTarget) + 1;
                        siblingIndex = dropTarget.transform.GetSiblingIndex() + 1;
                    }
                    else
                    {
                        index = m_treeView.IndexOf(dropTarget);
                        siblingIndex = dropTarget.transform.GetSiblingIndex();
                    }

                    obj.Parent = dropTarget.Parent;
                    obj.transform.SetParent(dropTarget.transform.parent);
                    obj.transform.SetSiblingIndex(siblingIndex);

                    TreeViewItemContainerData itemContainerData = (TreeViewItemContainerData)m_treeView.Insert(index, obj);
                    itemContainerData.Parent = treeViewItem.Parent;
                }
            }
            else
            {
                ExposeToEditor parent = null;

                if (obj.Parent != null)
                {
                    parent = obj.Parent;
                }
                if (m_treeView.IndexOf(obj) == -1)
                {
                    m_treeView.AddChild(parent, obj);
                }
            }

            m_isSpawningPrefab = false;
            */
        }

        private void OnObjectStarted(ExposeToEditor obj)
        {

        }

        private void OnObjectEnabled(ExposeToEditor obj)
        {
            VirtualizingTreeViewItem tvItem = m_treeView.GetTreeViewItem(obj);
            if (tvItem == null)
            {
                return;
            }
            Text text = tvItem.GetComponentInChildren<Text>();
            text.color = EnabledItemColor;
        }

        private void OnObjectDisabled(ExposeToEditor obj)
        {
            VirtualizingTreeViewItem tvItem = m_treeView.GetTreeViewItem(obj);
            if (tvItem == null)
            {
                return;
            }
            Text text = tvItem.GetComponentInChildren<Text>();
            text.color = DisabledItemColor;
        }

        private void OnObjectDestroying(ExposeToEditor o)
        {
            ExposeToEditor parent = null;
            bool isLastChild = false;
            if (o.Parent != null)
            {
                parent = o.Parent;
                isLastChild = (o.Parent.ChildCount - o.Parent.MarkedAsDestroyedChildCount) <= 1; //ChildCount is not yet decremented
            }

            m_treeView.RemoveChild(parent, o, isLastChild);
        }

        private void OnObjectDestroyed(ExposeToEditor o)
        {
           
        }

        private void OnObjectMarkAsDestoryedChanging(ExposeToEditor o)
        {
            if (o.MarkAsDestroyed)
            {
               
            }
            else
            {
                                
            }
        }

        private void OnObjectMarkAsDestroyedChanged(ExposeToEditor o)
        {
            if (o.MarkAsDestroyed)
            {
                ExposeToEditor parent = null;
                bool isLastChild = false;
                if (o.Parent != null)
                {
                    parent = o.Parent;
                    isLastChild = (o.Parent.ChildCount - o.Parent.MarkedAsDestroyedChildCount) <= 0;
                }

                m_treeView.RemoveChild(parent, o, isLastChild);
            }
            else
            {
                ExposeToEditor parent = null;

                if (o.Parent != null)
                {
                    parent = o.Parent;
                }

                m_treeView.AddChild(parent, o); //TODO: replace with Insert 

                ExposeToEditor nextSibling = o.NextSibling(Editor.Object.Get(true));
                if(nextSibling != null)
                {
                    m_treeView.SetPrevSibling(nextSibling, o);
                }   
            }
        }

        private void OnParentChanged(ExposeToEditor obj, ExposeToEditor oldParent, ExposeToEditor newParent)
        {
            if(!m_isStarted)
            {
                return;
            }
            if(Editor.IsPlaymodeStateChanging)
            {
                return;
            }
            
            bool isNewParentExpanded = true;
            bool isOldParentExpanded = true;
            bool isLastChild = false;
            if (newParent != null)
            {
                isNewParentExpanded = m_treeView.IsExpanded(newParent);
            }

            if (oldParent != null)
            {
                isLastChild = (oldParent.ChildCount - oldParent.MarkedAsDestroyedChildCount) <= 1;
                isOldParentExpanded = m_treeView.IsExpanded(oldParent);
            }

            if (isNewParentExpanded)
            {
                m_treeView.ChangeParent(newParent, obj);
                
                if (!isOldParentExpanded)
                {
                    if (isLastChild)
                    {
                        VirtualizingTreeViewItem oldParentContainer = m_treeView.GetTreeViewItem(oldParent);
                        if (oldParentContainer)
                        {
                            oldParentContainer.CanExpand = false;
                        }
                    }
                }
            }
            else
            {   
                if(newParent != null)
                {
                    VirtualizingTreeViewItem newParentTreeViewItem = m_treeView.GetTreeViewItem(newParent);
                    if(newParentTreeViewItem != null)
                    {
                        newParentTreeViewItem.CanExpand = true;
                    }
                }

                m_treeView.RemoveChild(oldParent, obj, isLastChild);
            }
        }

        private void OnNameChanged(ExposeToEditor obj)
        {
            VirtualizingTreeViewItem tvItem = m_treeView.GetTreeViewItem(obj);
            if (tvItem == null)
            {
                return;
            }
            Text text = tvItem.GetComponentInChildren<Text>();
            text.text = obj.name;
        }

        private bool CanDrop(object[] dragObjects)
        {
            IEnumerable<AssetItem> assetItems = dragObjects.OfType<AssetItem>();

            return assetItems.Count() > 0 && assetItems.Any(assetItem => m_project.ToType(assetItem) == typeof(GameObject));
        }

        public override void DragEnter(object[] dragObjects, PointerEventData pointerEventData)
        {
            base.DragEnter(dragObjects, pointerEventData);
            m_treeView.ExternalBeginDrag(pointerEventData.position);
        }

        public override void DragLeave(PointerEventData pointerEventData)
        {
            base.DragLeave(pointerEventData);
            m_treeView.ExternalItemDrop();
            Editor.DragDrop.SetCursor(KnownCursor.DropNowAllowed);
        }

        public override void Drag(object[] dragObjects, PointerEventData pointerEventData)
        {
            base.Drag(dragObjects, pointerEventData);
            m_treeView.ExternalItemDrag(pointerEventData.position);
            if (CanDrop(dragObjects))
            {
                Editor.DragDrop.SetCursor(KnownCursor.DropAllowed);
            }
            else
            {
                Editor.DragDrop.SetCursor(KnownCursor.DropNowAllowed);
                m_treeView.ClearTarget();
            }
        }

        public override void Drop(object[] dragObjects, PointerEventData pointerEventData)
        {
            base.Drop(dragObjects, pointerEventData);

            if (CanDrop(dragObjects))
            {
                ExposeToEditor dropTarget = (ExposeToEditor)m_treeView.DropTarget;
                VirtualizingTreeViewItem treeViewItem = null;
                if (dropTarget != null)
                {
                    treeViewItem = m_treeView.GetTreeViewItem(m_treeView.DropTarget);
                }
                
                AssetItem[] loadAssetItems = dragObjects.Where(o => o is AssetItem && m_project.ToType((AssetItem)o) == typeof(GameObject)).Select(o => (AssetItem)o).ToArray();
                if (loadAssetItems.Length > 0)
                {
                    m_isSpawningPrefab = true;
                    Editor.IsBusy = true;
                    m_project.Load(loadAssetItems, (error, objects) =>
                    {
                        Editor.IsBusy = false;
                        if (error.HasError)
                        {
                            IWindowManager wm = IOC.Resolve<IWindowManager>();
                            wm.MessageBox("Unable to load asset items.", error.ErrorText);
                            return;
                        }

                        GameObject[] createdObjects = new GameObject[objects.Length];
                        for (int i = 0; i < objects.Length; ++i)
                        {
                            GameObject prefab = (GameObject)objects[i];
                            bool wasPrefabEnabled = prefab.activeSelf;
                            prefab.SetActive(false);
                            GameObject prefabInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                            prefab.SetActive(wasPrefabEnabled);

                            ExposeToEditor exposeToEditor = prefabInstance.GetComponent<ExposeToEditor>();
                            if (exposeToEditor == null)
                            {
                                exposeToEditor = prefabInstance.AddComponent<ExposeToEditor>();
                            }

                            exposeToEditor.SetName(prefab.name);
                        
                            if(dropTarget == null)
                            {
                                exposeToEditor.Parent = null;
                                exposeToEditor.transform.SetParent(null);
                                m_treeView.Add(exposeToEditor);
                            }
                            else
                            {
                                if (m_treeView.DropAction == ItemDropAction.SetLastChild)
                                {
                                    exposeToEditor.Parent = dropTarget;
                                    exposeToEditor.transform.SetParent(dropTarget.transform);
                                    m_treeView.AddChild(dropTarget, exposeToEditor);
                                    treeViewItem.CanExpand = true;
                                    treeViewItem.IsExpanded = true;
                                }
                                else if (m_treeView.DropAction != ItemDropAction.None)
                                {
                                    int index;
                                    int siblingIndex;
                                    if (m_treeView.DropAction == ItemDropAction.SetNextSibling)
                                    {
                                        index = m_treeView.IndexOf(dropTarget) + 1;
                                        siblingIndex = dropTarget.transform.GetSiblingIndex() + 1;
                                    }
                                    else
                                    {
                                        index = m_treeView.IndexOf(dropTarget);
                                        siblingIndex = dropTarget.transform.GetSiblingIndex();
                                    }

                                    exposeToEditor.Parent = dropTarget.Parent;
                                    exposeToEditor.transform.SetParent(dropTarget.transform.parent != null ? dropTarget.transform.parent : null);
                                    exposeToEditor.transform.SetSiblingIndex(siblingIndex);

                                    TreeViewItemContainerData newTreeViewItemData = (TreeViewItemContainerData)m_treeView.Insert(index, exposeToEditor);
                                    VirtualizingTreeViewItem newTreeViewItem = m_treeView.GetTreeViewItem(exposeToEditor);
                                    if (newTreeViewItem != null)
                                    {
                                        newTreeViewItem.Parent = treeViewItem.Parent;
                                    }
                                    else
                                    {
                                        newTreeViewItemData.Parent = treeViewItem.Parent;
                                    }
                                }
                            }

                            prefabInstance.SetActive(true);
                            createdObjects[i] = prefabInstance;
                        }

                        if(createdObjects.Length > 0)
                        {
                            IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
                            editor.RegisterCreatedObjects(createdObjects);
                        }

                        m_treeView.ExternalItemDrop();
                        m_isSpawningPrefab = false;
                    });
                }
                else
                {
                    m_treeView.ExternalItemDrop();
                }
            }
            else
            {
                m_treeView.ExternalItemDrop();
            }
        }
    }
}

