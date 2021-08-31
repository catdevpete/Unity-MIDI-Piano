/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

#if UNITY_EDITOR
  public static class EditorResources {

    /// <summary>
    /// Finds all assets of a given type.  A simple utility wrapper around some
    /// AssetDatabase calls.
    /// </summary>
    public static T[] FindAllAssetsOfType<T>() where T : Object {
      return AssetDatabase.FindAssets("t:" + typeof(T).Name).
                           Select(guid => AssetDatabase.GUIDToAssetPath(guid)).
                           Select(path => AssetDatabase.LoadAssetAtPath<T>(path)).
                           ToArray();
    }

    /// <summary>
    /// Use like Resources.Load, but searches folders named EditorResources instead
    /// of folders named Resources.  Remember that you should not include the file
    /// extension, just like when using Resources!
    /// </summary>
    public static T Load<T>(string name) where T : Object {
      foreach (var rootDir in Directory.GetDirectories("Assets", "EditorResources", SearchOption.AllDirectories)) {
        string fullPath = Path.Combine(rootDir, name + ".dummy");
        string fullDir = Path.GetDirectoryName(fullPath);
        string fileName = Path.GetFileNameWithoutExtension(fullPath);

        if (!Directory.Exists(fullDir)) {
          continue;
        }

        foreach (var filename in Directory.GetFiles(fullDir, fileName + ".*")) {
          if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(filename))) {
            return AssetDatabase.LoadAssetAtPath<T>(filename);
          }
        }
      }
      return null;
    }
  }
#endif
}
