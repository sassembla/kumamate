using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleJson : MonoBehaviour
{
    // テスト用にjsonを保持、なんか左上に箱があるやつ
    public const string json = @"
{
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
			'components': {},
			'schemaVersion': 0,
			'styles': {}
		}
	}
}
";
}
