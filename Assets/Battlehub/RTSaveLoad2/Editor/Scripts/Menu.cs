﻿using Battlehub.RTCommon;
using Battlehub.RTCommon.EditorTreeView;
using Battlehub.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityObject = UnityEngine.Object;

namespace Battlehub.RTSaveLoad2
{
    public static class Menu
    {
      

        private static HashSet<UnityObject> ReadFromAssetLibraries(string[] guids, out int index, out AssetLibraryAsset asset, out AssetFolderInfo folder)
        {
            HashSet<UnityObject> hs = new HashSet<UnityObject>();

            List<AssetLibraryAsset> assetLibraries = new List<AssetLibraryAsset>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                AssetLibraryAsset assetLibrary = AssetDatabase.LoadAssetAtPath<AssetLibraryAsset>(path);
                if (assetLibrary != null)
                {
                    assetLibrary.Foreach(assetInfo =>
                    {
                        if (assetInfo.Object != null)
                        {
                            if (!hs.Contains(assetInfo.Object))
                            {
                                hs.Add(assetInfo.Object);
                            }

                            if(assetInfo.PrefabParts != null)
                            {
                                foreach (PrefabPartInfo prefabPart in assetInfo.PrefabParts)
                                {
                                    if (prefabPart.Object != null)
                                    {
                                        if (!hs.Contains(prefabPart.Object))
                                        {
                                            hs.Add(prefabPart.Object);
                                        }
                                    }
                                }
                            }
                        }
                        return true;
                    });

                    assetLibraries.Add(assetLibrary);
                }
            }

            if(assetLibraries.Count == 0)
            {
                asset = ScriptableObject.CreateInstance<AssetLibraryAsset>();
                index = 0;
            }
            else
            {
                asset = assetLibraries.OrderBy(a => a.AssetLibrary.Identity).FirstOrDefault();
                index = assetLibraries.Count - 1;
            }

            folder = asset.AssetLibrary.Folders.Where(f => f.depth == 0).First();
            if(folder.Assets == null)
            {
                folder.Assets = new List<AssetInfo>();
            }
            return hs;
        }

        private static void CreateAssetLibrary(object[] objects, string folderName, string assetLibraryName, int index, AssetLibraryAsset asset, AssetFolderInfo folder, HashSet<UnityObject> hs)
        {
            int identity = asset.AssetLibrary.Identity;
            
            foreach (UnityObject obj in objects)
            {
                if (!obj)
                {
                    continue;
                }

                if (hs.Contains(obj))
                {
                    continue; 
                }

                if (obj is GameObject)
                {
                    GameObject go = (GameObject)obj;
                    if (go.IsPrefab())
                    {
                        AssetInfo assetInfo = new AssetInfo(go.name, 0, identity);
                        assetInfo.Object = go;
                        identity++;

                        List<PrefabPartInfo> prefabParts = new List<PrefabPartInfo>();
                        AssetLibraryAssetsGUI.CreatePefabParts(go, ref identity, prefabParts);
                        if (prefabParts.Count >= AssetLibraryInfo.MAX_ASSETS - AssetLibraryInfo.INITIAL_ID)
                        {
                            EditorUtility.DisplayDialog("Unable Create AssetLibrary", string.Format("Max 'Indentity' value reached. 'Identity' ==  {0}", AssetLibraryInfo.MAX_ASSETS), "OK");
                            return;
                        }

                        if (identity >= AssetLibraryInfo.MAX_ASSETS)
                        {
                            SaveAssetLibrary(asset, folderName, assetLibraryName, index);
                            index++;

                            asset = ScriptableObject.CreateInstance<AssetLibraryAsset>();
                            folder = asset.AssetLibrary.Folders.Where(f => f.depth == 0).First();
                            if (folder.Assets == null)
                            {
                                folder.Assets = new List<AssetInfo>();
                            }
                            identity = asset.AssetLibrary.Identity;
                        }

                        assetInfo.PrefabParts = prefabParts;
                        asset.AssetLibrary.Identity = identity;
                        folder.Assets.Add(assetInfo);
                        assetInfo.Folder = folder;
                    }
                }
                else 
                {
                    AssetInfo assetInfo = new AssetInfo(obj.name, 0, identity);
                    assetInfo.Object = obj;
                    identity++;

                    if (identity >= AssetLibraryInfo.MAX_ASSETS)
                    {
                        SaveAssetLibrary(asset, folderName, assetLibraryName, index);
                        index++;

                        asset = ScriptableObject.CreateInstance<AssetLibraryAsset>();
                        folder = asset.AssetLibrary.Folders.Where(f => f.depth == 0).First();
                        if(folder.Assets == null)
                        {
                            folder.Assets = new List<AssetInfo>();
                        }
                        identity = asset.AssetLibrary.Identity;
                    }

                    asset.AssetLibrary.Identity = identity;
                    folder.Assets.Add(assetInfo);
                    assetInfo.Folder = folder;
                }
            }

            SaveAssetLibrary(asset, folderName, assetLibraryName, index);
            index++;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void SaveAssetLibrary(AssetLibraryAsset asset, string folderName, string assetLibraryName, int index)
        {
            string dir = BHPath.Root + "/RTSaveLoad2";
            string dataPath = Application.dataPath + "/";

            if (!Directory.Exists(dataPath + dir + "/Resources_Auto"))
            {
                AssetDatabase.CreateFolder("Assets/" + dir, "Resources_Auto");
            }

            dir = dir + "/Resources_Auto";
            if (!Directory.Exists(dataPath + dir + "/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/" + dir, "Resources");
            }

            dir = dir + "/Resources";

            string[] folderNameParts = folderName.Split('/');
            for (int i = 0; i < folderNameParts.Length; ++i)
            {
                string folderNamePart = folderNameParts[i];

                if (!Directory.Exists(dataPath + dir + "/" + folderNamePart))
                {
                    AssetDatabase.CreateFolder("Assets/" + dir, folderNamePart);
                }

                dir = dir + "/" + folderNamePart;
            }
            
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
            {
                if (index == 0)
                {

                    AssetDatabase.CreateAsset(asset, "Assets/" + dir + "/" + assetLibraryName + ".asset");
                }
                else
                {
                    AssetDatabase.CreateAsset(asset, "Assets/" + dir + "/" + assetLibraryName + (index + 1) + ".asset");
                }
            }
            
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static HashSet<UnityObject> ReadFromSceneAssetLibraries(Scene scene, out int index, out AssetLibraryAsset asset, out AssetFolderInfo folder)
        {
            string[] guids = AssetDatabase.FindAssets("", new[] { "Assets/" + BHPath.Root + "/RTSaveLoad2/Resources_Auto/Resources/Scenes/" + scene.name });
            return ReadFromAssetLibraries(guids, out index, out asset, out folder);
        }

        private static void CreateAssetLibraryFromScene(Scene scene, int index, AssetLibraryAsset asset, AssetFolderInfo folder, HashSet<UnityObject> hs)
        {
            TypeMap typeMap = new TypeMap();
            AssetDB assetDB = new AssetDB();

            IOC.Register<ITypeMap>(typeMap);
            IOC.Register<IAssetDB>(assetDB);

            PersistentRuntimeScene rtScene = new PersistentRuntimeScene();

            GetDepsFromContext ctx = new GetDepsFromContext();
            rtScene.GetDepsFrom(scene, ctx);

            IOC.Unregister<ITypeMap>(typeMap);
            IOC.Unregister<IAssetDB>(assetDB);

            CreateAssetLibrary(ctx.Dependencies.ToArray(), "Scenes/" + scene.name, "SceneAssetLibrary", index, asset, folder, hs);
        }

        private static HashSet<UnityObject> ReadFromBuiltInAssetLibraries(out int index, out AssetLibraryAsset asset, out AssetFolderInfo folder)
        {
            string[] guids = AssetDatabase.FindAssets("", new[] { "Assets/" + BHPath.Root + "/RTSaveLoad2/Resources_Auto/Resources/BuiltInAssets"});
            return ReadFromAssetLibraries(guids, out index, out asset, out folder);
        }

        private static void CreateBuiltInAssetLibrary(int index, AssetLibraryAsset asset, AssetFolderInfo folder, HashSet<UnityObject> hs)
        {
            Dictionary<string, Type> builtInExtra = new Dictionary<string, Type>
            {
                {  "Default-Line.mat", typeof(Material) },
                {  "Default-Material.mat", typeof(Material) },
                {  "Default-Particle.mat", typeof(Material) },
                {  "Default-Skybox.mat", typeof(Material) },
                {  "Sprites-Default.mat", typeof(Material) },
                {  "Sprites-Mask.mat", typeof(Material) },
                {  "UI/Skin/Background.psd", typeof(Sprite) },
                {  "UI/Skin/Checkmark.psd", typeof(Sprite) },
                {  "UI/Skin/DropdownArrow.psd", typeof(Sprite) },
                {  "UI/Skin/InputFieldBackground.psd", typeof(Sprite) },
                {  "UI/Skin/Knob.psd", typeof(Sprite) },
                {  "UI/Skin/UIMask.psd", typeof(Sprite) },
                {  "UI/Skin/UISprite.psd", typeof(Sprite) },
            };

            Dictionary<string, Type> builtIn = new Dictionary<string, Type>
            {
               { "New-Sphere.fbx", typeof(Mesh) },
               { "New-Capsule.fbx", typeof(Mesh) },
               { "New-Cylinder.fbx", typeof(Mesh) },
               { "Cube.fbx", typeof(Mesh) },
               { "New-Plane.fbx", typeof(Mesh) },
               { "Quad.fbx", typeof(Mesh) },
               { "Arial.ttf", typeof(Font) }
            };

            List<object> builtInAssets = new List<object>();
            foreach(KeyValuePair<string, Type> kvp in builtInExtra)
            {
                UnityObject obj = AssetDatabase.GetBuiltinExtraResource(kvp.Value, kvp.Key);
                if(obj != null)
                {
                    builtInAssets.Add(obj);
                }
            }

            foreach (KeyValuePair<string, Type> kvp in builtIn)
            {
                UnityObject obj = Resources.GetBuiltinResource(kvp.Value, kvp.Key);
                if (obj != null)
                {
                    builtInAssets.Add(obj);
                }
            }
            CreateAssetLibrary(builtInAssets.ToArray(), "BuiltInAssets", "BuiltInAssetLibrary", index, asset, folder, hs);
        }

        [MenuItem("Tools/Runtime SaveLoad2/Persistent Classes")]
        private static void ShowMenuItem()
        {
            PersistentClassMapperWindow.ShowWindow();
        }

        [MenuItem("Tools/Runtime SaveLoad2/Collect Scene Dependencies")]
        private static void AssetLibraryFromScene()
        {
            CreateBuiltInAssetLibrary();

            Scene scene = SceneManager.GetActiveScene();

            int index;
            AssetLibraryAsset asset;
            AssetFolderInfo folder;
            HashSet<UnityObject> hs = ReadFromBuiltInAssetLibraries(out index, out asset, out folder);
            HashSet<UnityObject> hs2 = ReadFromSceneAssetLibraries(scene, out index, out asset, out folder);

            foreach(UnityObject obj in hs)
            {
                if(!hs2.Contains(obj))
                {
                    hs2.Add(obj);
                }
            }

            CreateAssetLibraryFromScene(scene, index, asset, folder, hs2);
        }


        [MenuItem("Tools/Runtime SaveLoad2/Create BuiltInAssetLibrary")]
        private static void CreateBuiltInAssetLibrary()
        {
            int index;
            AssetLibraryAsset asset;
            AssetFolderInfo folder;
            HashSet<UnityObject> hs = ReadFromBuiltInAssetLibraries(out index, out asset, out folder);
            CreateBuiltInAssetLibrary(index, asset, folder, hs);
        }


        [MenuItem("Tools/Runtime SaveLoad2/Create Shader Profiles")]
        private static void CreateShaderProfiles()
        {
            RuntimeShaderProfilesGen.CreateProfile();
        }
    }

}

