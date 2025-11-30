#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
using ModularFramework;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// connect to steamworks for updating achievements
/// </summary>
[CreateAssetMenu(fileName = "SteamSystemSO_SO", menuName = "Game Module/Steam System")]
public class SteamSystemSO : GameModule<SteamSystemSO>,ILive
{
	[Header("Create steam_appid.txt at root folder"),SerializeField] private uint appId = 2906800;
	
	AppId_t AppId => (AppId_t)appId;
	[field: SerializeField] public bool Live { get; set; } = false;


	public SteamSystemSO()
	{
		updateMode = UpdateMode.EVERY_N_SECOND;
		everyNSecond = 10;
	}
	
#if !DISABLESTEAMWORKS
	
	public static bool Initialized {get; private set;}

	protected SteamAPIWarningMessageHook_t m_SteamAPIWarningMessageHook;

	[AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
	protected static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText) {
		Debug.LogWarning(pchDebugText);
	}

	protected override void OnAwake() { }

	protected override void OnStart() {
		if (!Packsize.Test()) {
			Debug.LogError("[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.", this);
		}

		if (!DllCheck.Test()) {
			Debug.LogError("[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.", this);
		}

		try {
			// If Steam is not running or the game wasn't started through Steam, SteamAPI_RestartAppIfNecessary starts the
			// Steam client and also launches this game again if the User owns it. This can act as a rudimentary form of DRM.
			// Note that this will run which ever version you have installed in steam. Which may not be the precise executable
			// we were currently running.

			// Once you get a Steam AppID assigned by Valve, you need to replace AppId_t.Invalid with it and
			// remove steam_appid.txt from the game depot. eg: "(AppId_t)480" or "new AppId_t(480)".
			// See the Valve documentation for more information: https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown
			if (SteamAPI.RestartAppIfNecessary(AppId)) {
				Debug.Log("[Steamworks.NET] Shutting down because RestartAppIfNecessary returned true. Steam will restart the application.");

				Application.Quit();
				return;
			}
		}
		catch (System.DllNotFoundException e) { // We catch this exception here, as it will be the first occurrence of it.
			Debug.LogError("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + e, this);

			Application.Quit();
			return;
		}

		// Initializes the Steamworks API.
		// If this returns false then this indicates one of the following conditions:
		// [*] The Steam client isn't running. A running Steam client is required to provide implementations of the various Steamworks interfaces.
		// [*] The Steam client couldn't determine the App ID of game. If you're running your application from the executable or debugger directly then you must have a [code-inline]steam_appid.txt[/code-inline] in your game directory next to the executable, with your app ID in it and nothing else. Steam will look for this file in the current working directory. If you are running your executable from a different directory you may need to relocate the [code-inline]steam_appid.txt[/code-inline] file.
		// [*] Your application is not running under the same OS user context as the Steam client, such as a different user or administration access level.
		// [*] Ensure that you own a license for the App ID on the currently active Steam account. Your game must show up in your Steam library.
		// [*] Your App ID is not completely set up, i.e. in Release State: Unavailable, or it's missing default packages.
		// Valve's documentation for this is located here:
		// https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown
		Initialized = SteamAPI.Init();
		if (!Initialized) {
			Debug.LogError("[Steamworks.NET] SteamAPI_Init() failed. Refer to Valve's documentation or the comment above this line for more information.", this);

			return;
		}
		if (m_SteamAPIWarningMessageHook == null) {
			// Set up our callback to receive warning messages from Steam.
			// You must launch with "-debug_steamapi" in the launch args to receive warnings.
			m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
			SteamClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);
		}
	}

	protected override void OnUpdate()
	{
		if (!Initialized) {
			return;
		}

		// Run Steam client callbacks
		SteamAPI.RunCallbacks();
	}


	// OnApplicationQuit gets called too early to shutdown the SteamAPI.
	// Because the SteamManager should be persistent and never disabled or destroyed we can shutdown the SteamAPI here.
	// Thus it is not recommended to perform any Steamworks work in other OnDestroy functions as the order of execution can not be garenteed upon Shutdown. Prefer OnDisable().
	protected override void OnSceneDestroy() {
		if (!Initialized) {
			return;
		}

		SteamAPI.Shutdown();
	}
	
	protected override void OnDraw() { }

#else
	public static bool Initialized {
		get {
			return false;
		}
	}
#endif // !DISABLESTEAMWORKS
	
	
	public void PublishAchievementToSteam(params string[] achievementAPI) {
		if(!Live) return;
		
		bool isAchievementUnlocked = false;
		if(Initialized) {
			foreach(var achievement in achievementAPI) {
				SteamUserStats.GetAchievement(achievement, out bool achieved);
				if(!achieved) {
					SteamUserStats.SetAchievement(achievement);
					isAchievementUnlocked = true;
				}
				if(achievement.Equals(Achievement.DAILY_1.ToString())) {
					// add to daily count
					SteamUserStats.GetStat("DAILY_COUNT", out int dailyCount);
					dailyCount ++;
					if(dailyCount<=3)
						SteamUserStats.SetStat("DAILY_COUNT", dailyCount);
					// no more than 3
					isAchievementUnlocked = true;
            
					if(dailyCount>=3) {
						SteamUserStats.GetAchievement(achievement, out bool hiddenAch);
						if(!hiddenAch) {
							SteamUserStats.SetAchievement(Achievement.DAILY_3.ToString());
							isAchievementUnlocked = true;
						}
					}
                    
				}
			}
			if(isAchievementUnlocked)
				SteamUserStats.StoreStats();
		}
	}
}
