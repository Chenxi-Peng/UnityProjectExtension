using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class FolderExplain : MonoBehaviour
{
    private static readonly Dictionary<string, string> ShowDic = new Dictionary<string, string>()
    {
        {"Art", "美术资源,全部放这里"},
        {"Arts", "美术资源,全部放这里"},
        {"Scripts", "脚本"},
        {"Script", "脚本"}
    };

    static FolderExplain()
    {
        EditorApplication.projectWindowItemOnGUI += HandleOnGUI;
        EditorApplication.projectWindowItemOnGUI += DrawLineOnGUI;
    }

    private static void HandleOnGUI(string guid, Rect selectionRect)
    {
        //缩略视图 不显示
        if (selectionRect.height > 16)
            return;

        var path = AssetDatabase.GUIDToAssetPath(guid);
        if (0 >= path.Length)
            return;

        var attr = File.GetAttributes(path);

        if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
            return;

        var nameRaw = Path.GetFileNameWithoutExtension(path);

        if (!ShowDic.ContainsKey(nameRaw))
            return;

        var _style = new GUIStyle(EditorStyles.miniLabel);

        _style.normal.textColor = Color.yellow;
        var extSize = _style.CalcSize(new GUIContent(ShowDic[nameRaw]));
        var nameSize = _style.CalcSize(new GUIContent(nameRaw));

        selectionRect.x += nameSize.x + 20;
        selectionRect.y += 2;

        selectionRect.width = extSize.x;
        selectionRect.height = extSize.y;

        var offsetRect = new Rect(selectionRect.position, selectionRect.size);
        EditorGUI.LabelField(offsetRect, ShowDic[nameRaw], _style);
    }

    public static Texture2D texture2d = new Texture2D(1, 1);
    private static readonly int xPixel = 10;
    private static readonly int yPixel = 8;
    private static readonly int longPixel = 8;
    private static readonly int nextLongPixel = 12;
    private static readonly Color colorLine = Color.white;

    private static void DrawLineOnGUI(string guid, Rect selectionRect)
    {
        //return;

        //缩略视图 不显示
        if (selectionRect.height > 16)
            return;

        //根据Rect的数值是否为14 判定当前是否为预览窗口
        if (selectionRect.x <= 14)
            return;

        var path = AssetDatabase.GUIDToAssetPath(guid);
        if (0 >= path.Length)
            return;

        var attr = File.GetAttributes(path);

        if(!IsSingleColumnView())
        {
            if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                return;
        }


        //DebugLog.LogWarning($"{path} rectt is {selectionRect}");

        var name = path;
        int count = name.Length - name.Replace("/", "").Length;
        var nameRaw = Path.GetFileNameWithoutExtension(path);

        if (nameRaw == "Assets" && count == 0)
            return;

        var _style = new GUIStyle(EditorStyles.miniLabel);

        _style.normal.textColor = colorLine;

        var nameSize = _style.CalcSize(new GUIContent(nameRaw));

        Vector2 selRect = Vector2.zero;
        selRect.x = xPixel * count;
        selRect.y = selectionRect.y += yPixel;

        //绘制水平线
        Vector2 horLineStart = new Vector2(selRect.x, selRect.y);
        Vector2 horLineEnd = new Vector2(selRect.x + longPixel, selRect.y);
        Handles.DrawBezier(horLineStart, horLineEnd,
            horLineStart, horLineEnd, colorLine, texture2d, 1);

        if (IsSingleColumnView())
        {
            if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
            {
                Vector2 nextHorLineStart = new Vector2(horLineEnd.x + 1, selRect.y);
                Vector2 nextHorLineEnd = new Vector2(horLineEnd.x + 1 + nextLongPixel, selRect.y);

                Handles.DrawBezier(nextHorLineStart, nextHorLineEnd,
                    nextHorLineStart, nextHorLineEnd, colorLine, texture2d, 1);
            }
        }
        else
        {
            //判定文件夹是否包含文件夹，不包含多绘制一次水平线
            if (!isHaveDirectories(path))
            {
                Vector2 nextHorLineStart = new Vector2(horLineEnd.x + 1, selRect.y);
                Vector2 nextHorLineEnd = new Vector2(horLineEnd.x + 1 + nextLongPixel, selRect.y);

                Handles.DrawBezier(nextHorLineStart, nextHorLineEnd,
                    nextHorLineStart, nextHorLineEnd, colorLine, texture2d, 1);
            }
        }

        //绘制竖线
        Vector2 verticalLineStart = new Vector2(selRect.x, selectionRect.y - yPixel);
        Vector2 verticalLineEnd = new Vector2(selRect.x, selectionRect.y + yPixel);
        Handles.DrawBezier(verticalLineStart, verticalLineEnd,
            verticalLineStart, verticalLineEnd, colorLine, texture2d, 1);

        //多次绘制竖线
        for(int i = 1; i < count; i++)
        {
            selRect.x = xPixel * i;

            verticalLineStart = new Vector2(selRect.x, selectionRect.y - yPixel);
            verticalLineEnd = new Vector2(selRect.x, selectionRect.y + yPixel);

            Handles.DrawBezier(verticalLineStart, verticalLineEnd,
                verticalLineStart, verticalLineEnd, colorLine, texture2d, 1);
        }

    }


    /// <summary>
    /// 子文件夹
    /// </summary>
    /// <param name="path">指定文件夹的路径</param>
    private static bool isHaveDirectories(string path)
    {
        try
        {
            string[] dir = Directory.GetDirectories(path); //文件夹列表

            if (dir.Length != 0) //当前目录文件或文件夹不为空                   
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// 判定窗口是否为OneColumn
    /// </summary>
    /// <returns></returns>
    private static bool IsSingleColumnView()
    {
        var projectWindow = GetProjectWindow();
        var columnsCount = (int)projectWindow.GetType().GetField("m_ViewMode", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(projectWindow);
        return columnsCount == 0;
    }


    private static EditorWindow GetProjectWindow()
    {
        if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.titleContent.text == "Project")
        {
            return EditorWindow.focusedWindow;
        }

        EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        foreach (EditorWindow item in windows)
        {
            if (item.titleContent.text == "Project")
            {
                return item;
            }
        }

        return default;
    }
}
