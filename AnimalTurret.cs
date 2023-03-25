#region License (GPL v2)
/*
    AnimalTurret - Make Rust autoturrets target animals (again)
    Copyright (c) 2021-2023 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License v2.0.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License (GPL v2)
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AnimalTurret", "RFC1920", "1.0.5")]
    [Description("Make (npc)autoturrets target animals in range")]
    internal class AnimalTurret : RustPlugin
    {
        private ConfigData configData;
        private List<uint> disabledTurrets = new List<uint>();
        public static AnimalTurret Instance;
        private bool enabled;

        [PluginReference]
        private readonly Plugin Friends, Clans, RustIO;

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Message(Lang(key, player.Id, args));

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["enabled"] = "Animal targeting enabled for this autoturret",
                ["disabled"] = "Animal targeting disabled for this autoturret"
            }, this);
        }
        #endregion

        #region Commands
        [ChatCommand("noat")]
        private void cmdDisableTurret(BasePlayer player, string command, string[] args)
        {
            List<AutoTurret> foundTurrets = new List<AutoTurret>();
            Vis.Entities(player.transform.position, 3f, foundTurrets);
            foreach (AutoTurret t in foundTurrets)
            {
                if (!disabledTurrets.Contains(t.net.ID))
                {
                    if (player.userID == t.OwnerID || IsFriend(player.userID, t.OwnerID))
                    {
                        disabledTurrets.Add(t.net.ID);
                        AnimalTarget at = t.gameObject.GetComponent<AnimalTarget>();
                        if (at != null) UnityEngine.Object.Destroy(at);
                        SaveData();
                        Message(player.IPlayer, "disabled");
                    }
                }
            }
        }

        [ChatCommand("doat")]
        private void cmdEnableTurret(BasePlayer player, string command, string[] args)
        {
            List<AutoTurret> foundTurrets = new List<AutoTurret>();
            Vis.Entities(player.transform.position, 3f, foundTurrets);
            foreach(AutoTurret t in foundTurrets)
            {
                if (disabledTurrets.Contains(t.net.ID))
                {
                    if (player.userID == t.OwnerID || IsFriend(player.userID, t.OwnerID))
                    {
                        disabledTurrets.Remove(t.net.ID);
                        t.gameObject.AddComponent<AnimalTarget>();
                        SaveData();
                        Message(player.IPlayer, "enabled");
                    }
                }
            }
        }

        private bool IsFriend(ulong playerid, ulong ownerid)
        {
            if (!configData.HonorRelationships) return false;

            if (configData.useFriends && Friends != null)
            {
                object fr = Friends?.CallHook("AreFriends", playerid, ownerid);
                if (fr != null && (bool)fr)
                {
                    return true;
                }
            }
            if (configData.useClans && Clans != null)
            {
                string playerclan = (string)Clans?.CallHook("GetClanOf", playerid);
                string ownerclan = (string)Clans?.CallHook("GetClanOf", ownerid);
                if (playerclan != null && ownerclan != null)
                {
                    if (playerclan == ownerclan)
                    {
                        return true;
                    }
                }
            }
            if (configData.useTeams)
            {
                BasePlayer player = BasePlayer.FindByID(playerid);
                if (player != null && player.currentTeam != 0)
                {
                    RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                    if (playerTeam != null)
                    {
                        if (playerTeam.members.Contains(ownerid))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        #endregion

        private void OnServerInitialized()
        {
            Instance = this;
            LoadConfigVariables();
            LoadData();
            enabled = true;
            List<uint> processed = new List<uint>();
            if (configData.npcTurrets)
            {
                foreach (NPCAutoTurret t in UnityEngine.Object.FindObjectsOfType<NPCAutoTurret>())
                {
                    if (!processed.Contains(t.net.ID))
                    {
                        if (configData.debug) Puts($"Arming NPC turret {t.net.ID} for animal targeting.");
                        t.gameObject.AddComponent<AnimalTargetNPC>();
                        processed.Add(t.net.ID);
                    }
                }
            }

            foreach (AutoTurret t in UnityEngine.Object.FindObjectsOfType<AutoTurret>())
            {
                if (disabledTurrets.Contains(t.net.ID)) continue;
                if (configData.defaultEnabled && !processed.Contains(t.net.ID))
                {
                    if (configData.debug) Puts($"Arming turret {t.net.ID} for animal targeting.");
                    t.gameObject.AddComponent<AnimalTarget>();
                    processed.Add(t.net.ID);
                }
            }
        }

        private void Unload()
        {
            foreach (AutoTurret t in UnityEngine.Object.FindObjectsOfType<AutoTurret>())
            {
                if (t != null)
                {
                    AnimalTarget at = t.gameObject.GetComponent<AnimalTarget>();
                    if (at != null) UnityEngine.Object.Destroy(at);
                }
            }

            foreach (NPCAutoTurret t in UnityEngine.Object.FindObjectsOfType<NPCAutoTurret>())
            {
                if (t != null)
                {
                    AnimalTargetNPC at = t.gameObject.GetComponent<AnimalTargetNPC>();
                    if (at != null) UnityEngine.Object.Destroy(at);
                }
            }
        }

        private void OnEntitySpawned(NPCAutoTurret turret)
        {
            if (!enabled) return;
            if (configData.npcTurrets)
            {
                if (turret == null) return;
                turret.gameObject.AddComponent<AnimalTargetNPC>();
            }
        }

        private void OnEntitySpawned(AutoTurret turret)
        {
            if (!enabled) return;
            if (turret.OwnerID == 0) return;
            BasePlayer player = BasePlayer.Find(turret.OwnerID.ToString());

            if (configData.defaultEnabled)
            {
                turret.gameObject.AddComponent<AnimalTarget>();
                Message(player.IPlayer, "enabled");
            }
            else
            {
                Message(player.IPlayer, "disabled");
            }
        }

        private void LoadData()
        {
            disabledTurrets = Interface.Oxide.DataFileSystem.ReadObject<List<uint>>(Name + "/disabledTurrets");
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "/disabledTurrets", disabledTurrets);
        }

        private class AnimalTargetNPC : MonoBehaviour
        {
            private NPCAutoTurret turret;

            private void Awake()
            {
                turret = GetComponent<NPCAutoTurret>();
                //turret.SetFlag(BaseEntity.Flags.Reserved1, true); // If false they will attack players regardless of hostility.
                if (turret != null) InvokeRepeating("FindTargets", 5f, 1.0f);
            }

            internal void FindTargets()
            {
                if (Instance.disabledTurrets.Contains(turret.net.ID)) return;
                if (turret.target == null)
                {
                    List<BaseAnimalNPC> localpig = new List<BaseAnimalNPC>();
                    Vis.Entities(turret.eyePos.transform.position, 30f, localpig);

                    foreach (BaseAnimalNPC bce in localpig)
                    {
                        if (string.IsNullOrEmpty(bce.ShortPrefabName)) continue;
                        if (turret.ObjectVisible(bce) && !Instance.configData.exclusions.Contains(bce.ShortPrefabName))
                        {
                            if (Instance.configData.debug) Instance.Puts($"NPCAutoturret {turret.net.ID} targeting {bce.ShortPrefabName}({bce.net.ID})");
                            turret.target = bce;
                            bce.MarkHostileFor();
                            break;
                        }
                    }
                }
            }
        }

        private class AnimalTarget : MonoBehaviour
        {
            private AutoTurret turret;

            private void Awake()
            {
                turret = GetComponent<AutoTurret>();
                if (turret != null) InvokeRepeating("FindTargets", 5f, 1.0f);
            }

            internal void FindTargets()
            {
                if (Instance.disabledTurrets.Contains(turret.net.ID)) return;
                if (turret.target == null && turret.IsPowered())
                {
                    List<BaseAnimalNPC> localpig = new List<BaseAnimalNPC>();
                    Vis.Entities(turret.eyePos.transform.position, 30f, localpig);

                    foreach (BaseAnimalNPC bce in localpig)
                    {
                        if (string.IsNullOrEmpty(bce.ShortPrefabName)) continue;
                        if (turret.ObjectVisible(bce) && !Instance.configData.exclusions.Contains(bce.ShortPrefabName))
                        {
                            if (Instance.configData.debug) Instance.Puts($"Autoturret {turret.net.ID} targeting {bce.ShortPrefabName}({bce.net.ID})");
                            turret.target = bce;
                            if (!turret.PeacekeeperMode())
                            {
                                bce.MarkHostileFor();
                            }
                            break;
                        }
                    }
                }
            }
        }

        #region config
        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                defaultEnabled = true,
                npcTurrets = false,
                HonorRelationships = false,
                useFriends = false,
                useClans = false,
                useTeams = false,
                exclusions = new List<string>() { "chicken" },
                Version = Version
            };
            SaveConfig(config);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Animal targeting enabled by default")]
            public bool defaultEnabled;

            [JsonProperty(PropertyName = "Animal targeting by NPC AutoTurrets")]
            public bool npcTurrets;

            [JsonProperty(PropertyName = "Animals to exclude")]
            public List<string> exclusions;

            [JsonProperty(PropertyName = "Honor Friends/Clans/Teams for commands")]
            public bool HonorRelationships;

            [JsonProperty(PropertyName = "Use Friends plugins for commands")]
            public bool useFriends;

            [JsonProperty(PropertyName = "Use Clans plugins for commands")]
            public bool useClans;

            [JsonProperty(PropertyName = "Use Rust teams for commands")]
            public bool useTeams;

            public bool debug;
            public VersionNumber Version;
        }
        #endregion
    }
}
