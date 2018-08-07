using System.Collections.Generic;
using ProtoBuf;
using Battlehub.RTSaveLoad2;
using UnityEngine;
using UnityEngine.Battlehub.SL2;

using UnityObject = UnityEngine.Object;
namespace Battlehub.RTSaveLoad2
{
    public partial class TypeMap
    {
        public TypeMap()
        {
            m_toPeristentType.Add(typeof(UnityObject), typeof(PersistentObject));
            m_toUnityType.Add(typeof(PersistentObject), typeof(UnityObject));
            m_toPeristentType.Add(typeof(Mesh), typeof(PersistentMesh));
            m_toUnityType.Add(typeof(PersistentMesh), typeof(Mesh));
            m_toPeristentType.Add(typeof(GameObject), typeof(PersistentGameObject));
            m_toUnityType.Add(typeof(PersistentGameObject), typeof(GameObject));
            m_toPeristentType.Add(typeof(MeshFilter), typeof(PersistentMeshFilter));
            m_toUnityType.Add(typeof(PersistentMeshFilter), typeof(MeshFilter));
            m_toPeristentType.Add(typeof(Transform), typeof(PersistentTransform));
            m_toUnityType.Add(typeof(PersistentTransform), typeof(Transform));
            m_toPeristentType.Add(typeof(Vector3), typeof(PersistentVector3));
            m_toUnityType.Add(typeof(PersistentVector3), typeof(Vector3));
            m_toPeristentType.Add(typeof(Quaternion), typeof(PersistentQuaternion));
            m_toUnityType.Add(typeof(PersistentQuaternion), typeof(Quaternion));
            
        }
    }
}
