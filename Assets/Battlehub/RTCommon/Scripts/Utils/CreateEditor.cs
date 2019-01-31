﻿using Battlehub.RTCommon;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTCommon
{
    public interface IRTEState
    {
        bool IsCreated
        {
            get;
        }

        event Action<IRTE> Created;
        event Action<IRTE> Destroyed;
    }

    [DefaultExecutionOrder(-100)]
    public class CreateEditor : MonoBehaviour, IRTEState
    {
        public event Action<IRTE> Created;
        public event Action<IRTE> Destroyed;

        public bool IsCreated
        {
            get { return m_editor != null; }
        }

        [SerializeField]
        private Button m_createEditorButton = null;
        [SerializeField]
        private RTEBase m_editorPrefab = null;

        private RTEBase m_editor;
          
        private void Awake()
        {
            IOC.RegisterFallback<IRTEState>(this);
            m_editor = (RTEBase)FindObjectOfType(m_editorPrefab.GetType());
            if(m_editor != null)
            {
                if(m_editor.IsOpened)
                {
                    m_editor.IsOpenedChanged += OnIsOpenedChanged;
                    gameObject.SetActive(false);
                }
            }
            if(Created != null)
            {
                Created(m_editor);
            }
            m_createEditorButton.onClick.AddListener(OnOpen);
        }

        private void OnDestroy()
        {
            IOC.UnregisterFallback<IRTEState>(this);
            if(m_createEditorButton != null)
            {
                m_createEditorButton.onClick.RemoveListener(OnOpen);
            }
            if(m_editor != null)
            {
                m_editor.IsOpenedChanged -= OnIsOpenedChanged;
            }
        }

        private void OnOpen()
        {
            m_editor = Instantiate(m_editorPrefab);
            m_editor.name = "RuntimeEditor";
            m_editor.IsOpenedChanged += OnIsOpenedChanged;
            m_editor.transform.SetAsFirstSibling();
            gameObject.SetActive(false);
        }

        private void OnIsOpenedChanged()
        {
            if(m_editor != null)
            {
                m_editor.IsOpenedChanged -= OnIsOpenedChanged;
            }
            
            if(this != null)
            {
                gameObject.SetActive(true);
            }

            Destroy(m_editor);

            if(Destroyed != null)
            {
                Destroyed(m_editor);
            }
        }
    }
}

