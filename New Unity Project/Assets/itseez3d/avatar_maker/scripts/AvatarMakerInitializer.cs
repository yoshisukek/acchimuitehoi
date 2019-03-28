/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, January 2019
*/

using Coroutines;
using ItSeez3D.AvatarSdk.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ItSeez3D.AvatarMaker
{
	public class AvatarMakerInitializer
	{
		private static bool isInitialized = false;

		private static bool isExportEnabled = false;

		private static CoroutineHandle initRoutine;

		private static IAvatarProvider avatarProvider = null;

		public static bool IsInitialized
		{
			get { return isInitialized; }
		}

		public static bool IsInitializationInProgress
		{
			get { return initRoutine.IsRunning; }
		}

		public static IAvatarProvider AvatarProvider
		{
			get { return avatarProvider; }
		}

		public static bool IsExportEnabled
		{
			get { return isExportEnabled; }
		}

		public static void StartInitialization()
		{
			if (!AvatarSdkMgr.IsInitialized)
			{
				Debug.Log("AvatarSdkMgr.Init");
				AvatarSdkMgr.Init(sdkType: SdkType.Offline);
			}

			if (avatarProvider == null)
			{
				avatarProvider = AvatarSdkMgr.IoCContainer.Create<IAvatarProvider>();
				initRoutine = EditorRunner.instance.Run(InitRoutine());
			}
		}

		public static bool IsPlatformSupported()
		{
#if UNITY_EDITOR_WIN
			return true;
#else
			return false;
#endif
		}

		private static IEnumerator InitRoutine()
		{
			yield return avatarProvider.InitializeAsync();
			isExportEnabled = DllHelperCore.IsExportEnabled();
			isInitialized = true;
		}

		
	}
}
