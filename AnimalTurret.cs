#region License (GPL v3)
/*
    NextGenPVE - Prevent damage to players and objects in a Rust PVE environment
    Copyright (c) 2020 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License (GPL v3)
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AnimalTurret", "RFC1920", "1.0.1")]
    [Description("Make autoturrets target animals in range")]
    internal class AnimalTurret : RustPlugin
    {
        private ConfigData configData;
        private List<uint> disabledTurrets = new List<uint>();
        public static AnimalTurret Instance;

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
            foreach (var t in foundTurrets)
            {
                if (!disabledTurrets.Contains(t.net.ID))
                {
                    disabledTurrets.Add(t.net.ID);
                    var at = t.gameObject.GetComponent<AnimalTarget>();
                    if (at != null) UnityEngine.Object.Destroy(at);
                    SaveData();
                    Message(player.IPlayer, "disabled");
                }
            }
        }

        [ChatCommand("doat")]
        private void cmdEnableTurret(BasePlayer player, string command, string[] args)
        {
            List<AutoTurret> foundTurrets = new List<AutoTurret>();
            Vis.Entities(player.transform.position, 3f, foundTurrets);
            foreach(var t in foundTurrets)
            {
                if (disabledTurrets.Contains(t.net.ID))
                {
                    disabledTurrets.Remove(t.net.ID);
                    t.gameObject.AddComponent<AnimalTarget>();
                    SaveData();
                    Message(player.IPlayer, "enabled");
                }
            }
        }
        #endregion

        void OnServerInitialized()
        {
            Instance = this;
            LoadConfigVariables();
            LoadData();
            var turrets = UnityEngine.Object.FindObjectsOfType<AutoTurret>();
            foreach(var t in turrets)
            {
                if (disabledTurrets.Contains(t.net.ID)) continue;
                if (configData.defaultEnabled)
                {
                    t.gameObject.AddComponent<AnimalTarget>();
                }
            }
        }

        void Unload()
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<AutoTurret>();
            foreach(var t in turrets)
            {
                if (t != null)
                {
                    var at = t.gameObject.GetComponent<AnimalTarget>();
                    if (at != null) UnityEngine.Object.Destroy(at);
                }
            }
        }

        void OnEntitySpawned(AutoTurret turret)
        {
            if (turret.OwnerID == 0) return;
            var player = BasePlayer.Find(turret.OwnerID.ToString());

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

        class AnimalTarget : MonoBehaviour
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

                    foreach (BaseCombatEntity bce in localpig)
                    {
                        if (string.IsNullOrEmpty(bce.ShortPrefabName)) continue;
                        if (turret.ObjectVisible(bce) && !Instance.configData.exclusions.Contains(bce.ShortPrefabName))
                        {
                            turret.target = bce;
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
            var config = new ConfigData
            {
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
            public bool defaultEnabled = true;

            [JsonProperty(PropertyName = "Animals to exclude")]
            public List<string> exclusions;

            public VersionNumber Version;
        }
        #endregion
    }
}
