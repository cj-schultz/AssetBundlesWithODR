﻿using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_IOS_ON_DEMAND_RESOURCES
using UnityEngine.iOS;
#endif
using System.Collections;

namespace AssetBundles
{
	public abstract class AssetBundleLoadOperation : IEnumerator
	{
		public object Current
		{
			get
			{
				return null;
			}
		}
		public bool MoveNext()
		{
			return !IsDone();
		}
		
		public void Reset()
		{
		}
		
		abstract public bool Update ();
		
		abstract public bool IsDone ();
	}

	public abstract class AssetBundleDownloadOperation: AssetBundleLoadOperation
	{
		bool done;

		public string assetBundleName { get; private set; }
		public LoadedAssetBundle assetBundle { get; protected set; }
		public string error { get; protected set; }

		protected abstract bool downloadIsDone { get; }
		protected abstract void FinishDownload();

		public override bool Update()
		{
			if (!done && downloadIsDone)
			{
				FinishDownload();
				done = true;
			}

			return !done;
		}

		public override bool IsDone()
		{
			return done;
		}

		public AssetBundleDownloadOperation(string assetBundleName)
		{
			this.assetBundleName = assetBundleName;
		}
	}

#if ENABLE_IOS_ON_DEMAND_RESOURCES
	public class AssetBundleDownloadFromODROperation: AssetBundleDownloadOperation
	{
		OnDemandResourcesRequest request;

		public AssetBundleDownloadFromODROperation(string assetBundleName)
			: base(assetBundleName)
		{
			request = OnDemandResources.PreloadAsync(new string[] { assetBundleName });
		}

		protected override bool downloadIsDone { get { return (request == null) || request.isDone; } }

		protected override void FinishDownload()
		{
			error = request.error;
			if (error != null)
				return;

			var path = "res://" + assetBundleName;
			var bundle = AssetBundle.LoadFromFile(path); 
			if (bundle == null)
			{
				error = string.Format("Failed to load {0}", path);
				request.Dispose();
			}
			else
			{
				assetBundle = new LoadedAssetBundle(bundle);
				// At the time of unload request is already set to null, so capture it to local variable.
				var localRequest = request;
				// Dispose of request only when bundle is unloaded to keep the ODR pin alive.
				assetBundle.unload += () =>
					{
						localRequest.Dispose();
					};
			}

			request = null;
		}
	}
#endif

#if ENABLE_IOS_APP_SLICING
	// Read asset bundle synchronously from an asset catalog
	public class AssetBundleOpenFromAssetCatalogOperation: AssetBundleDownloadOperation
	{		
		public AssetBundleOpenFromAssetCatalogOperation(string assetBundleName)
			: base(assetBundleName)
		{
			var path = "res://" + assetBundleName;
			var bundle = AssetBundle.LoadFromFile(path); 
			if (bundle == null)
				error = string.Format("Failed to load {0}", path);
			else
				assetBundle = new LoadedAssetBundle(bundle);
		}
		
		protected override bool downloadIsDone { get { return true; } }
		
		protected override void FinishDownload() {}
	}
#endif

	public class AssetBundleDownloadFromWebOperation: AssetBundleDownloadOperation
	{
		WWW www;

		public AssetBundleDownloadFromWebOperation(string assetBundleName, WWW www)
			: base(assetBundleName)
		{
			if (www == null)
				throw new System.ArgumentNullException("www");

			this.www = www;
		}

		protected override bool downloadIsDone { get { return (www == null) || www.isDone; } }

		protected override void FinishDownload()
		{
			error = www.error;
			if (!string.IsNullOrEmpty(error))
				return;

			AssetBundle bundle = www.assetBundle;
			if (bundle == null)
				error = string.Format("{0} is not valid asset bundle.", assetBundleName);
			else
				assetBundle = new LoadedAssetBundle(www.assetBundle);

			www.Dispose();
			www = null;
		}
	}

	#if UNITY_EDITOR
	public class AssetBundleLoadLevelSimulationOperation : AssetBundleLoadOperation
	{	
		AsyncOperation m_Operation = null;
	
	
		public AssetBundleLoadLevelSimulationOperation (string assetBundleName, string levelName, bool isAdditive)
		{
			string[] levelPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, levelName);
			if (levelPaths.Length == 0)
			{
				///@TODO: The error needs to differentiate that an asset bundle name doesn't exist
				//        from that there right scene does not exist in the asset bundle...
				
				Debug.LogError("There is no scene with name \"" + levelName + "\" in " + assetBundleName);
				return;
			}
			
			if (isAdditive)
				m_Operation = UnityEditor.EditorApplication.LoadLevelAdditiveAsyncInPlayMode(levelPaths[0]);
			else
				m_Operation = UnityEditor.EditorApplication.LoadLevelAsyncInPlayMode(levelPaths[0]);
		}
		
		public override bool Update ()
		{
			return false;
		}
		
		public override bool IsDone ()
		{		
			return m_Operation == null || m_Operation.isDone;
		}
	}
	
	#endif
	public class AssetBundleLoadLevelOperation : AssetBundleLoadOperation
	{
		protected string 				m_AssetBundleName;
		protected string 				m_LevelName;
		protected bool 					m_IsAdditive;
		protected string 				m_DownloadingError;
		protected AsyncOperation		m_Request;
	
		public AssetBundleLoadLevelOperation (string assetbundleName, string levelName, bool isAdditive)
		{
			m_AssetBundleName = assetbundleName;
			m_LevelName = levelName;
			m_IsAdditive = isAdditive;
		}
	
		public override bool Update ()
		{
			if (m_Request != null)
				return false;
			
			LoadedAssetBundle bundle = AssetBundleManager.GetLoadedAssetBundle (m_AssetBundleName, out m_DownloadingError);
			if (bundle != null)
			{
				if (m_IsAdditive)
                {
                    m_Request = SceneManager.LoadSceneAsync(m_LevelName, LoadSceneMode.Additive);
                }
				else
                {
                    m_Request = SceneManager.LoadSceneAsync(m_LevelName, LoadSceneMode.Single);
                }

				return false;
			}
			else
            {
				return true;
            }
		}
		
		public override bool IsDone ()
		{
			// Return if meeting downloading error.
			// m_DownloadingError might come from the dependency downloading.
			if (m_Request == null && m_DownloadingError != null)
			{
				Debug.LogError(m_DownloadingError);
				return true;
			}
			
			return m_Request != null && m_Request.isDone;
		}
	}
	
	public abstract class AssetBundleLoadAssetOperation : AssetBundleLoadOperation
	{
		public abstract T GetAsset<T>() where T : UnityEngine.Object;
	}
	
	public class AssetBundleLoadAssetOperationSimulation : AssetBundleLoadAssetOperation
	{
		Object							m_SimulatedObject;
		
		public AssetBundleLoadAssetOperationSimulation (Object simulatedObject)
		{
			m_SimulatedObject = simulatedObject;
		}
		
		public override T GetAsset<T>()
		{
			return m_SimulatedObject as T;
		}
		
		public override bool Update ()
		{
			return false;
		}
		
		public override bool IsDone ()
		{
			return true;
		}
	}
	
	public class AssetBundleLoadAssetOperationFull : AssetBundleLoadAssetOperation
	{
		protected string 				m_AssetBundleName;
		protected string 				m_AssetName;
		protected string 				m_DownloadingError;
		protected System.Type 			m_Type;
		protected AssetBundleRequest	m_Request = null;
	
		public AssetBundleLoadAssetOperationFull (string bundleName, string assetName, System.Type type)
		{
			m_AssetBundleName = bundleName;
			m_AssetName = assetName;
			m_Type = type;
		}
		
		public override T GetAsset<T>()
		{
			if (m_Request != null && m_Request.isDone)
				return m_Request.asset as T;
			else
				return null;
		}
		
		// Returns true if more Update calls are required.
		public override bool Update ()
		{
			if (m_Request != null)
				return false;
	
			LoadedAssetBundle bundle = AssetBundleManager.GetLoadedAssetBundle (m_AssetBundleName, out m_DownloadingError);
			if (bundle != null)
			{
				///@TODO: When asset bundle download fails this throws an exception...
				m_Request = bundle.m_AssetBundle.LoadAssetAsync (m_AssetName, m_Type);
				return false;
			}
			else
			{
				return true;
			}
		}
		
		public override bool IsDone ()
		{
			// Return if meeting downloading error.
			// m_DownloadingError might come from the dependency downloading.
			if (m_Request == null && m_DownloadingError != null)
			{
				Debug.LogError(m_DownloadingError);
				return true;
			}
	
			return m_Request != null && m_Request.isDone;
		}
	}
	
	public class AssetBundleLoadManifestOperation : AssetBundleLoadAssetOperationFull
	{
		public AssetBundleLoadManifestOperation (string bundleName, string assetName, System.Type type)
			: base(bundleName, assetName, type)
		{
		}
	
		public override bool Update ()
		{
			base.Update();
			
			if (m_Request != null && m_Request.isDone)
			{
				AssetBundleManager.AssetBundleManifestObject = GetAsset<AssetBundleManifest>();
				return false;
			}
			else
				return true;
		}
	}
	
	
}
