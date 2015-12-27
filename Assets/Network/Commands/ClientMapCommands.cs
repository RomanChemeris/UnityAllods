﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ClientCommands
{
    [Serializable()]
    public struct AddPlayer : IClientCommand
    {
        public int ID;
        public string Name;
        public int Color;
        public long Money;
        public Dictionary<int, DiplomacyFlags> Diplomacy;
        public bool Silent; // whether to display the "player has connected" or not.
        public bool ConsolePlayer; // this is true when the server says us that we have control over this one.

        public bool Process()
        {
            if (MapLogic.Instance.GetPlayerByID(ID) != null)
                return true; // don't disconnect, who knows what happened... just skip this player

            MapLogicPlayer player = new MapLogicPlayer((ServerClient)null);
            player.ID = ID;
            player.Name = Name;
            player.Color = Color;
            player.Money = Money;
            foreach (var pair in Diplomacy)
                player.Diplomacy[pair.Key] = pair.Value;
            if (ConsolePlayer)
            {
                GameConsole.Instance.WriteLine("We are controlling player {0}.", player.Name);
                MapLogic.Instance.ConsolePlayer = player;
            }
            MapLogic.Instance.AddNetPlayer(player, Silent);
            return true;
        }
    }

    [Serializable()]
    public struct DelPlayer : IClientCommand
    {
        public int ID;
        public bool Kick; // whether the "player was kicked" message will be displayed (if Silent is false)
        public bool Silent;

        public bool Process()
        {
            MapLogicPlayer player = MapLogic.Instance.GetPlayerByID(ID);
            if (player == MapLogic.Instance.ConsolePlayer)
                MapLogic.Instance.ConsolePlayer = null;
            if (player != null)
                MapLogic.Instance.DelNetPlayer(player, Silent, Kick);
            return true;
        }
    }
}