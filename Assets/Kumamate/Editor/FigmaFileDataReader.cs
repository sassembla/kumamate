using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kumamate
{
    public static class FigmaFileDataReader
    {
        /*

'name': 'Untitled',
'lastModified': '2021-06-28T05:12:27Z',
'thumbnailUrl': 'https://s3-alpha-sig.figma.com/thumbnails/e3c0d957-f8a1-4394-b542-9c658cea6f1c?Expires=1626048000&Signature=TIkalICPjMRBYotcimc1G3wQHPVsqI8eaJ4FMCuUrA9mXFNP9op6~iB80Zk9sNAcIarOi1DScTYbkg4HVkhWZFyO7BYxuSldPWoXpjO91o86bsup5N5N5DkmQDFnOzXluVIjFYg6zvVO9iVCJjpjcf0CudCAKIiOLJlhHxDmJApyWqPIjkmUVcHKiNkDZF6T2fY7UHFid9ovEL1Can-YB-9DyPyCLiINqp1w8tYJQsbAoN9f7yHsT09Hi1vY9bXTV2IXeJMD8GAwQv~Va-7n2tGF-rBSOaCKdxEP9wzeuwEEeYViz2v1PKx8lS0SyAN098jvspGSVgzrctJyXQUK1w__&Key-Pair-Id=APKAINTVSUGEWH5XD5UA',
'version': '943187479',
'role': 'owner',
'nodes': {
    '1:2': {
        'document': {
            'id': '1:2',
            'name': 'iPhone 11 Pro / X - 1',
            'type': 'FRAME',
            'blendMode': 'PASS_THROUGH',
            'children': [{
                'id': '1:3',
                'name': 'Rectangle 1',
                'type': 'RECTANGLE',
                'blendMode': 'PASS_THROUGH',
                'absoluteBoundingBox': {
                    'x': -168.0,
                    'y': -320.0,
                    'width': 203.0,
                    'height': 213.0
                },
                'constraints': {
                    'vertical': 'TOP',
                    'horizontal': 'LEFT'
                },
                'fills': [{
                    'blendMode': 'NORMAL',
                    'type': 'SOLID',
                    'color': {
                        'r': 0.76862746477127075,
                        'g': 0.76862746477127075,
                        'b': 0.76862746477127075,
                        'a': 1.0
                    }
                }],
                'strokes': [],
                'strokeWeight': 1.0,
                'strokeAlign': 'INSIDE',
                'effects': []
            }],
            'absoluteBoundingBox': {
                'x': -188.0,
                'y': -407.0,
                'width': 375.0,
                'height': 812.0
            },
            'constraints': {
                'vertical': 'TOP',
                'horizontal': 'LEFT'
            },
            'clipsContent': true,
            'background': [{
                'blendMode': 'NORMAL',
                'type': 'SOLID',
                'color': {
                    'r': 1.0,
                    'g': 1.0,
                    'b': 1.0,
                    'a': 1.0
                }
            }],
            'fills': [{
                'blendMode': 'NORMAL',
                'type': 'SOLID',
                'color': {
                    'r': 1.0,
                    'g': 1.0,
                    'b': 1.0,
                    'a': 1.0
                }
            }],
            'strokes': [],
            'strokeWeight': 1.0,
            'strokeAlign': 'INSIDE',
            'backgroundColor': {
                'r': 1.0,
                'g': 1.0,
                'b': 1.0,
                'a': 1.0
            },
            'effects': []
        },
        */

        // frame要素を読んでいく
        public static FigmaFrameData ReadFrame(Dictionary<string, object> frameDict)
        {
            var id = frameDict["id"] as string;
            var name = frameDict["name"] as string;

            var absoluteBoundingBox = frameDict["absoluteBoundingBox"] as Dictionary<string, object>;
            var absRect = ReadAbsBoundingBox(absoluteBoundingBox);
            var startPosDiff = new Vector2(absRect.X, absRect.Y);

            var rect = new FigmaRect();
            rect.X = 0f;
            rect.Y = 0f;
            rect.Width = absRect.Width;
            rect.Height = absRect.Height;

            var childContents = new List<FigmaContent>();

            // まだ内包要素がある場合読みにいく
            if (frameDict.ContainsKey("children"))
            {
                var children = frameDict["children"] as List<object>;
                foreach (var child in children)
                {
                    var childDict = child as Dictionary<string, object>;
                    var childContent = ReadContent(childDict, startPosDiff);
                    childContents.Add(childContent);
                }
            }

            var frame = new FigmaFrameData();
            frame.Identifier = FileNameConstructor.ConstructFileName(name, id);
            frame.AbsRect = rect;
            frame.Children.AddRange(childContents.ToArray());

            return frame;
        }

        // frameの下の要素を読んでいく
        private static FigmaContent ReadContent(Dictionary<string, object> contentDict, Vector2 startPosDiff)
        {
            var id = contentDict["id"] as string;
            var name = contentDict["name"] as string;

            var absoluteBoundingBox = contentDict["absoluteBoundingBox"] as Dictionary<string, object>;
            var absRect = ReadAbsBoundingBox(absoluteBoundingBox);

            // uGUIでは、yの値は-が下なので-に変換している。
            var rect = new FigmaRect();
            rect.X = absRect.X - startPosDiff.x;
            rect.Y = -(absRect.Y - startPosDiff.y);
            rect.Width = absRect.Width;
            rect.Height = absRect.Height;

            var type = contentDict["type"] as string;

            var childContents = new List<FigmaContent>();

            // まだ内包要素がある場合読みにいく
            if (contentDict.ContainsKey("children"))
            {
                var children = contentDict["children"] as List<object>;

                foreach (var child in children)
                {
                    var childDict = child as Dictionary<string, object>;
                    var childContent = ReadContent(childDict, startPosDiff);
                    childContents.Add(childContent);
                }
            }

            switch (type)
            {
                case "TEXT":
                    {
                        // テキストなので、必要な情報をセット
                        var characters = string.Empty;
                        var fontName = string.Empty;
                        var fontPostScriptName = string.Empty;
                        var fontSize = 0;
                        var fontWeight = 0;

                        var lineHeightPercent = 0f;

                        var r = 0f;
                        var g = 0f;
                        var b = 0f;
                        var a = 0f;

                        foreach (var t in contentDict)
                        {
                            switch (t.Key)
                            {
                                case "characters":
                                    characters = t.Value as string ?? string.Empty;
                                    break;
                                case "style":
                                    var styleDict = t.Value as Dictionary<string, object>;

                                    fontName = styleDict["fontFamily"] as string ?? string.Empty;
                                    fontPostScriptName = styleDict["fontPostScriptName"] as string ?? string.Empty;

                                    fontWeight = Convert.ToInt32(styleDict["fontWeight"]);

                                    fontSize = Convert.ToInt32(styleDict["fontSize"]);
                                    lineHeightPercent = Convert.ToSingle(styleDict["lineHeightPercent"]);
                                    break;
                                case "fills":
                                    var fillsList = t.Value as List<object>;
                                    foreach (var fill in fillsList)
                                    {
                                        var fillDict = fill as Dictionary<string, object>;
                                        if (fillDict.ContainsKey("color"))
                                        {
                                            var colorDict = fillDict["color"] as Dictionary<string, object>;
                                            r = Convert.ToSingle(colorDict["r"]);
                                            g = Convert.ToSingle(colorDict["g"]);
                                            b = Convert.ToSingle(colorDict["b"]);
                                            a = Convert.ToSingle(colorDict["a"]);
                                        }
                                    }
                                    break;

                                // case "background":// なんか辞書が入ってる
                                // case "backgroundColor":// 背景色
                                // case "visible":// これfalseだったら扱わなくてよくない？
                                // case "cornerRadius":
                                // case "styles":
                                //     var stylesDict = t.Value as Dictionary<string, object>;
                                //     foreach (var styles in stylesDict)
                                //     {
                                //         Debug.Log("styles:" + styles.Key + " v:" + styles.Value);
                                //     }
                                //     break;
                                case "id":
                                case "name":
                                case "type":
                                case "absoluteBoundingBox":
                                case "children":
                                case "opacity":// 無視して良さそうなシリーズ
                                case "blendMode":
                                case "constraints":
                                case "strokeAlign":
                                case "strokeWeight":
                                case "strokeCap":
                                case "strokes":
                                case "clipsContent":
                                case "characterStyleOverrides":
                                case "layoutAlign":
                                case "layoutMode":
                                case "layoutGrow":
                                case "layoutVersion":
                                case "primaryAxisAlignItems":
                                case "styleOverrideTable":
                                case "booleanOperation":
                                case "componentId":
                                case "effects":
                                case "strokeJoin":
                                case "itemSpacing":
                                case "locked":
                                case "counterAxisAlignItems":
                                case "rectangleCornerRadii":
                                    break;

                                default:
                                    // Debug.Log("key:" + t + " val:" + contentDict[t]);
                                    break;
                            }
                        }

                        var text = new FigmaText();
                        text.Characters = characters;
                        text.FontName = fontName;
                        text.FontPostScriptName = fontPostScriptName;
                        text.FontSize = fontSize;
                        text.FontWeight = fontWeight;
                        text.LineHeightPercent = lineHeightPercent;
                        text.R = r;
                        text.G = g;
                        text.B = b;
                        text.A = a;

                        var contentWithText = new FigmaContent();
                        contentWithText.Type = type;
                        contentWithText.Id = FileNameConstructor.ConstructFileName(name, id);
                        contentWithText.AbsRect = rect;
                        contentWithText.Children.AddRange(childContents.ToArray());
                        contentWithText.Text = text;

                        return contentWithText;
                    }
                case "RECTANGLE":
                    {
                        var r = 0f;
                        var g = 0f;
                        var b = 0f;
                        var a = 0f;
                        foreach (var t in contentDict)
                        {
                            switch (t.Key)
                            {
                                case "fills":
                                    var fillsList = t.Value as List<object>;
                                    foreach (var fill in fillsList)
                                    {
                                        var fillDict = fill as Dictionary<string, object>;
                                        if (fillDict.ContainsKey("color"))
                                        {
                                            var colorDict = fillDict["color"] as Dictionary<string, object>;
                                            r = Convert.ToSingle(colorDict["r"]);
                                            g = Convert.ToSingle(colorDict["g"]);
                                            b = Convert.ToSingle(colorDict["b"]);
                                            a = Convert.ToSingle(colorDict["a"]);
                                        }
                                    }
                                    break;
                                // case "background":
                                //     var backgroundList = t.Value as List<object>;
                                //     foreach (var background in backgroundList)
                                //     {
                                //         var backgroundDict = background as Dictionary<string, object>;
                                //         if (backgroundDict.ContainsKey("color"))
                                //         {
                                //             var colorDict = backgroundDict["color"] as Dictionary<string, object>;
                                //             bgr = Convert.ToSingle(colorDict["r"]);
                                //             bgg = Convert.ToSingle(colorDict["g"]);
                                //             bgb = Convert.ToSingle(colorDict["b"]);
                                //             bga = Convert.ToSingle(colorDict["a"]);
                                //         }
                                //     }
                                //     break;
                                // case "backgroundColor":
                                //     break;
                                default:
                                    // Debug.Log("frame key:" + t.Key + " v:" + t.Value);
                                    break;
                            }
                        }
                        var rectangle = new FigmaRectangle();
                        rectangle.R = r;
                        rectangle.G = g;
                        rectangle.B = b;
                        rectangle.A = a;

                        var contentWithRect = new FigmaContent();
                        contentWithRect.Type = type;
                        contentWithRect.Id = FileNameConstructor.ConstructFileName(name, id);
                        contentWithRect.AbsRect = rect;
                        contentWithRect.Children.AddRange(childContents.ToArray());
                        contentWithRect.Rectangle = rectangle;

                        return contentWithRect;
                    }

                case "ELLIPSE":
                    var ellipseContent = new FigmaContent();
                    foreach (var t in contentDict)
                    {
                        switch (t.Key)
                        {
                            default:
                                // TODO: 後で何か拾えるものがあったら拾う
                                // Debug.Log("unhandled t.Key:" + t.Key);
                                break;
                        }
                    }
                    ellipseContent.Type = type;
                    ellipseContent.Id = FileNameConstructor.ConstructFileName(name, id);
                    ellipseContent.AbsRect = rect;
                    ellipseContent.Children.AddRange(childContents.ToArray());
                    return ellipseContent;
                default:
                    var content = new FigmaContent();
                    // type, name, id, rect, childContents.ToArray()
                    content.Type = type;
                    content.Id = FileNameConstructor.ConstructFileName(name, id);
                    content.AbsRect = rect;
                    content.Children.AddRange(childContents.ToArray());
                    return content;
            }
        }

        private static FigmaRect ReadAbsBoundingBox(Dictionary<string, object> boundingBoxDict)
        {
            var x = Convert.ToSingle(boundingBoxDict["x"]);
            var y = Convert.ToSingle(boundingBoxDict["y"]);
            var width = Convert.ToSingle(boundingBoxDict["width"]);
            var height = Convert.ToSingle(boundingBoxDict["height"]);
            var figmaRect = new FigmaRect();
            figmaRect.X = x;
            figmaRect.Y = y;
            figmaRect.Width = width;
            figmaRect.Height = height;
            return figmaRect;
        }

        // 一つ~複数のframe要素を含んでいるcanvas要素を読んでいく
        public static FigmaFrameData[] ReadCanvas(Dictionary<string, object> frameDict)
        {
            var frames = new List<FigmaFrameData>();

            var children = frameDict["children"] as List<object>;
            foreach (var child in children)
            {
                var childDict = child as Dictionary<string, object>;
                var frame = ReadFrame(childDict);
                frames.Add(frame);
            }

            return frames.ToArray();
        }

    }
}