/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, January 2019
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ItSeez3D.AvatarMaker.WebCamera;
using ItSeez3D.AvatarMaker.MotionCapture;

namespace ItSeez3D.AvatarMaker.Editor
{
	public class MotionCaptureWindow : BaseWindow
	{
		private AnimationClip animationClip = null;
		private bool isCapturing = false;
		private string capturingButtonLabel = string.Empty;
		private string recordingButtonLabel = string.Empty;
		private string capturingErrorLabel = string.Empty;
		private string recordingErrorLabel = string.Empty;
		private float animationTime = 0.0f;
		private bool isAutoPlayAnimation = true;
		private int cameraId = 0;

		private AvatarInfo avatarInfo = null;

		private AvatarAnimator avatarAnimator = null;

		[MenuItem("Window/Avatar Maker/Facial Motion Capture")]
		static void Init()
		{
			var window = (MotionCaptureWindow)EditorWindow.GetWindow(typeof(MotionCaptureWindow));
			window.titleContent.text = "Facial Capture";
			window.minSize = new Vector2(480, 550);
			window.Show();
		}

		private void OnEnable()
		{
			if (!AvatarMakerInitializer.IsPlatformSupported())
			{
				Debug.LogError("Avatar plugin supports only Windows platform and works in the Editor mode.");
				return;
			}

			if (!AvatarMakerInitializer.IsInitialized)
				AvatarMakerInitializer.StartInitialization();
		}

		void OnGUI()
		{
			InitUI();

			GUILayout.Label("Facial Motion Capture", titleStyle);

			if (!AvatarMakerInitializer.IsPlatformSupported())
			{
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Avatar plugin supports only Windows platform and works in the Editor mode.");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				return;
			}

			if (AvatarMakerInitializer.IsInitializationInProgress)
			{
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label(EditorApplication.isPlaying ? "Exit play mode to load SDK" : "Loading...");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				return;
			}

			WebCamDevice[] cameraDevices = WebCamTexture.devices;
			if (cameraDevices != null && cameraDevices.Length > 0)
			{
				if (!isCapturing)
				{
					if (cameraDevices.Length > 1)
					{
						string[] cameraNames = cameraDevices.Select(d => { return d.name; }).ToArray();
						cameraId = GUILayout.SelectionGrid(cameraId, cameraNames, 1, "toggle");
					}
					else
						cameraId = 0;
				}

				capturingButtonLabel = isCapturing ? "Stop capturing" : "Start capturing";
				isCapturing = GUILayout.Toggle(isCapturing, capturingButtonLabel, "Button");
			}
			else
			{
				EditorGUILayout.HelpBox("There is no available web camera.", MessageType.Info);
			}

			if (isCapturing)
			{
				capturingErrorLabel = string.Empty;
				if (avatarAnimator == null)
				{
					avatarInfo = FindAvatarObject();
					if (avatarInfo != null)
					{
						avatarAnimator = new AvatarAnimator(avatarInfo.transform, avatarInfo.headMeshRenderer, cameraOffset);
						isCapturing = avatarAnimator.StartCapturing(WebCamTexture.devices[cameraId].name, avatarInfo.code);
						if (!isCapturing)
						{
							capturingErrorLabel = "Unable to start motion capture.";
							Debug.LogError(capturingErrorLabel);
							avatarAnimator = null;
							return;
						}
					}
					else
					{
						isCapturing = false;
						return;
					}

					if (AvatarAnimator.RecordAtStart)
						StartRecording();

					if (AnimationMode.InAnimationMode())
						ToggleAnimationMode();
				}
				Texture2D tex = avatarAnimator.HandleCapturedFrame();
				DisplayFrameTexture(tex);
			}
			else
			{
				if (avatarAnimator != null)
				{
					avatarAnimator.StopCapturing();
					avatarAnimator = null;
				}
			}

			GUILayout.Space(20);

			EditorGUILayout.BeginVertical("Box");
			{
				EditorGUILayout.LabelField("Recording options", titleStyle);
				GUILayout.Space(5);
				if (isCapturing)
				{
					recordingButtonLabel = avatarAnimator.IsRecording ? "Stop recording" : "Start recording";
					if (avatarAnimator.IsRecording != GUILayout.Toggle(avatarAnimator.IsRecording, recordingButtonLabel, "Button"))
					{
						if (avatarAnimator.IsRecording)
							avatarAnimator.FinishRecording();
						else
							StartRecording();
					}
					GUILayout.Space(5);
				}

				AvatarAnimator.RecordAtStart = GUILayout.Toggle(AvatarAnimator.RecordAtStart, "Record at start");

				animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation file: ", animationClip, typeof(AnimationClip), false);

				AvatarAnimator.ApplyTranslation = GUILayout.Toggle(AvatarAnimator.ApplyTranslation, "Capture translation");
				AvatarAnimator.ApplyRotation = GUILayout.Toggle(AvatarAnimator.ApplyRotation, "Capture rotation");
			}
			EditorGUILayout.EndVertical();
			GUILayout.Space(10);

			if (animationClip != null && !isCapturing)
			{
				EditorGUILayout.BeginVertical("Box");
				{
					EditorGUILayout.LabelField("Playback", titleStyle);
					GUILayout.Space(5);

					EditorGUI.BeginChangeCheck();
					GUILayout.Toggle(AnimationMode.InAnimationMode(), "Play recorded animation");
					if (EditorGUI.EndChangeCheck())
						ToggleAnimationMode();

					if (AnimationMode.InAnimationMode())
					{
						isAutoPlayAnimation = GUILayout.Toggle(isAutoPlayAnimation, "Automatically play in loop");

						animationTime = EditorGUILayout.Slider(animationTime, 0.0f, animationClip.length);
					}
				}
				EditorGUILayout.EndVertical();
			}

			if (!string.IsNullOrEmpty(capturingErrorLabel))
				EditorGUILayout.HelpBox(capturingErrorLabel, MessageType.Error);

			if (!string.IsNullOrEmpty(recordingErrorLabel))
			{
				EditorGUILayout.HelpBox(recordingErrorLabel, MessageType.Error);
				ShowAvatarMakerProLink();
			}

			if (isCapturing)
				Repaint();
		}

		float lastUpdateTime = 0;
		private void Update()
		{
			if (animationClip == null)
				return;

			if (AnimationMode.InAnimationMode())
			{
				if (avatarInfo == null)
				{
					avatarInfo = FindAvatarObject();
					if (avatarInfo == null)
					{
						ToggleAnimationMode();
						return;
					}
				}

				AnimationMode.BeginSampling();
				AnimationMode.SampleAnimationClip(avatarInfo.gameObject, animationClip, animationTime);
				AnimationMode.EndSampling();
				SceneView.RepaintAll();

				if (isAutoPlayAnimation)
				{
					animationTime += (Time.realtimeSinceStartup - lastUpdateTime);
					if (animationTime >= animationClip.length)
						animationTime = 0.0f;
					Repaint();
				}
			}
			lastUpdateTime = Time.realtimeSinceStartup;
		}

		private void DisplayFrameTexture(Texture2D cameraTexture)
		{
			float previewAspect = (float)cameraTexture.height / (float)cameraTexture.width;
			int newWidth = (int)Mathf.Min(position.width, cameraTexture.width);
			Vector2 previewSize = new Vector2(newWidth, (previewAspect * newWidth));
			GUI.DrawTexture(new Rect(new Vector2((position.width - previewSize.x) * 0.5f, 50.0f), previewSize), cameraTexture);
			GUILayout.Space((int)Mathf.Max(20, previewSize.y + 10));
		}

		private AvatarInfo FindAvatarObject()
		{
			capturingErrorLabel = string.Empty;

			List<Object> avatarInfoObjects = GameObject.FindObjectsOfType(typeof(AvatarInfo)).ToList();
			if (avatarInfoObjects.Count == 0)
			{
				capturingErrorLabel = "There are no avatars on the scene to animate!";
				Debug.LogError(capturingErrorLabel);
				return null;
			}
			if (avatarInfoObjects.Count > 1)
			{
				capturingErrorLabel = "There are multiple avatars on the scene! Motion capture works only for a single avatar.";
				Debug.LogError(capturingErrorLabel);
				return null;
			}

			return avatarInfoObjects[0] as AvatarInfo;
		}

		private void StartRecording()
		{
			if (!avatarAnimator.IsRecordingEnabled)
			{
				recordingErrorLabel = "You are allowed to record animation only in the Avatar Maker Pro version!";
				return;
			}

			if (animationClip == null)
				animationClip = CreateAnimationFile();
			avatarAnimator.StartRecording(animationClip);
		}

		private AnimationClip CreateAnimationFile()
		{
			string animationsFolder = "itseez3d_animations";
			string animationsFolderWithAssets = string.Format("Assets/{0}", animationsFolder);
			if (!AssetDatabase.IsValidFolder(animationsFolderWithAssets))
				AssetDatabase.CreateFolder("Assets", animationsFolder);

			int idx = 0;
			string animationName = string.Empty;
			while(true)
			{
				animationName = string.Format("avatar_animation_{0}", idx);
				if (AssetDatabase.FindAssets(animationName).Length == 0)
					break;
				idx++;
			}

			AnimationClip animation = new AnimationClip();
			string animationFileName = string.Format("{0}/{1}.anim", animationsFolderWithAssets, animationName);
			AssetDatabase.CreateAsset(animation, animationFileName);

			return animation;
		}

		private void ToggleAnimationMode()
		{
			if (AnimationMode.InAnimationMode())
				AnimationMode.StopAnimationMode();
			else
				AnimationMode.StartAnimationMode();
		}
	}
}
