﻿/*
Changing Room - a GTA V Mod for changing models and outfits
Copyright(C) 2015 Matthias C. M. Troffaes

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License along
with this program; if not, write to the Free Software Foundation, Inc.,
51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using GTA;
using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

// we abstract away the difference between component variations, props, head overlays, etc.
// they are all called "slots", and where each slot has a type (e.g. prop) and an id (e.g. prop_id)
// each slot can be assigned three integer values (e.g. drawable, texture, palette, or could be something else)
public enum SlotType
{
    CompVar,
    Prop,
    HeadOverlay,
}

public struct SlotKey
{
    public SlotType typ; // compvar, prop, overlay, ...
    public int id;  // component_id, prop_id, overlay_id, ...

    public SlotKey(SlotType typ_, int id_)
    {
        typ = typ_;
        id = id_;
    }

    public SlotKey(XmlTextReader reader)
    {
        typ = (SlotType)Enum.Parse(typeof(SlotType), reader.GetAttribute("slot_type"));
        id = int.Parse(reader.GetAttribute("slot_id"));
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("slot_type", typ.ToString());
        writer.WriteAttributeString("slot_id", id.ToString());
    }
}

public struct SlotValue
{
    public int index1, index2, index3;

    public SlotValue(int i1, int i2, int i3)
    {
        index1 = i1;
        index2 = i2;
        index3 = i3;
    }

    public SlotValue(XmlTextReader reader)
    {
        index1 = int.Parse(reader.GetAttribute("index1"));
        index2 = int.Parse(reader.GetAttribute("index2"));
        index3 = int.Parse(reader.GetAttribute("index3"));
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("index1", index1.ToString());
        writer.WriteAttributeString("index2", index2.ToString());
        writer.WriteAttributeString("index3", index3.ToString());
    }
}

public class PedData
{
    private static readonly Dictionary<SlotType, int> slot_type_num_id = new Dictionary<SlotType, int>
    {
        [SlotType.CompVar] = 12,
        [SlotType.Prop] = 8,
        [SlotType.HeadOverlay] = 13,
    };

    private Dictionary<SlotKey, SlotValue> data = new Dictionary<SlotKey, SlotValue>();

    public void WriteXml(XmlWriter writer)
    {
        foreach (var item in data)
        {
            writer.WriteStartElement("SetSlotValue");
            item.Key.WriteXml(writer);
            item.Value.WriteXml(writer);
            writer.WriteEndElement();
        }
    }

    private readonly int[] mp_male_clear_drawable =
    {
        0, 0, 0, 3, 11, 0, 13, 0, 15, 0, 0, 15
    };

    private readonly int[] mp_female_clear_drawable =
    {
        0, 0, 0, 8, 13, 0, 12, 0, 14, 0, 0, 82
    };

    public SlotValue GetSlotValue(SlotKey slot)
    {
        if (data.ContainsKey(slot))
            return data[slot];
        else
            return new SlotValue(0, 0, 0);
    }

    public static int GetNumId(SlotType typ)
    {
        return slot_type_num_id[typ];
    }

    public static int GetNumIndex1(Ped ped, SlotKey key)
    {
        switch (key.typ)
        {
            case SlotType.CompVar:
                return Math.Max(1, Function.Call<int>(
                    Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, ped.Handle, key.id));
            case SlotType.Prop:
                // no prop = extra drawable variation in this script
                return Math.Max(1, Function.Call<int>(
                    Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS, ped.Handle, key.id) + 1);
            case SlotType.HeadOverlay:
                // no overlay = extra overlay in this script
                return Math.Max(1, Function.Call<int>(
                    Hash._GET_NUM_HEAD_OVERLAY_VALUES, key.id) + 1);
            default:
                return 1;
        }
    }

    public int GetNumIndex2(Ped ped, SlotKey key)
    {
        var slot_value = GetSlotValue(key);
        return GetNumIndex2(ped, key, slot_value.index1);
    }

    public static int GetNumIndex2(Ped ped, SlotKey key, int index1)
    {
        switch (key.typ)
        {
            case SlotType.CompVar:
                // drawable_id = index1
                return Function.Call<int>(
                    Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, ped.Handle, key.id, index1);
            case SlotType.Prop:
                if (index1 == 0)
                    // no prop, so no textures
                    return 1;
                else
                {
                    // drawable_id = index1 - 1
                    var num = Function.Call<int>(
                        Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS, ped.Handle, key.id, index1 - 1);
                    // sometimes returns -1 or 0 if there are no textures
                    return (num <= 0) ? 1 : num;

                }
            case SlotType.HeadOverlay:
                if (index1 == 0)
                    // clear overlay
                    return 1;
                else
                    // opacity: 0 -> 1.0, 7 -> 0.125, in steps of 0.125
                    return 7;
            default:
                return 1;
        }
    }

    public int GetNumIndex3(Ped ped, SlotKey key)
    {
        var slot_value = GetSlotValue(key);
        return GetNumIndex3(ped, key, slot_value.index1, slot_value.index2);
    }

    public static int GetNumIndex3(Ped ped, SlotKey key, int index1, int index2)
    {
        return 1;
    }

    public void SetSlotValue(Ped ped, SlotKey slot_key, SlotValue slot_value)
    {
        switch (slot_key.typ)
        {
            case SlotType.CompVar:
                Function.Call(
                    Hash.SET_PED_COMPONENT_VARIATION,
                    ped.Handle,
                    slot_key.id, slot_value.index1, slot_value.index2, slot_value.index3);
                break;
            case SlotType.Prop:
                if (slot_value.index1 == 0)
                    Function.Call(Hash.CLEAR_PED_PROP, ped.Handle, slot_key.id);
                else
                    Function.Call(Hash.SET_PED_PROP_INDEX, ped.Handle, slot_key.id, slot_value.index1 - 1, slot_value.index2, true);
                break;
            case SlotType.HeadOverlay:
                if (slot_value.index1 == 0)
                    Function.Call(Hash.SET_PED_HEAD_OVERLAY, ped.Handle, slot_key.id, 0, 0.0f);
                else
                    Function.Call(Hash.SET_PED_HEAD_OVERLAY, ped.Handle, slot_key.id, slot_value.index1 - 1, (8 - slot_value.index2) / 8.0f);
                break;
        }
        if (slot_value.index1 == 0 && slot_value.index2 == 0 && slot_value.index3 == 0)
            data.Remove(slot_key);
        else
            data[slot_key] = slot_value;
    }

    public void ClearSlotValue(Ped ped, SlotKey slot_key)
    {
        var slot_value = new SlotValue(0, 0, 0);
        if (slot_key.typ == SlotType.CompVar)
        {
            var hash = (PedHash)ped.Model.Hash;
            if (hash == PedHash.FreemodeMale01)
                slot_value.index1 = mp_male_clear_drawable[slot_key.id];
            else if (hash == PedHash.FreemodeFemale01)
                slot_value.index1 = mp_female_clear_drawable[slot_key.id];
        }
        SetSlotValue(ped, slot_key, slot_value);
    }

    public void ChangePlayerModel(PedHash hash)
    {
        var model = new Model(hash);
        // only in recent script hook
        /*
        if (!Game.Player.ChangeModel(model))
        {
            UI.Notify("could not request model");
        }
        */
        if (!model.IsInCdImage || !model.IsPed || !model.Request(1000))
        {
            UI.Notify("could not request model");
        }
        else
        {
            Function.Call(Hash.SET_PLAYER_MODEL, Game.Player, model.Hash);
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Game.Player.Character.Handle);
            if (hash == PedHash.FreemodeMale01 || hash == PedHash.FreemodeFemale01)
                // must call this otherwise head overlays don't work
                Function.Call(Hash.SET_PED_HEAD_BLEND_DATA, Game.Player.Character.Handle, 0, 0, 0, 0, 0, 0, 0.0f, 0.0f, 0.0f, true);
            data.Clear();
        }
        model.MarkAsNoLongerNeeded();
    }
}

public class ChangingRoom : Script
{
    static int UI_LIST_MAX = 256;
    static List<dynamic> UI_LIST = Enumerable.Range(0, UI_LIST_MAX).Cast<dynamic>().ToList();
    private readonly Dictionary<string, PedHash> _pedhash;
    private MenuPool menuPool;

    // all named slot key names, both sp and mp characters
    // the mapping to actual slot keys is defined further
    public enum SlotKeyName
    {
        // component variations
        Face,
        Beard,
        Haircut,
        Shirt,
        SubShirt,
        Pants,
        Hands,
        Shoes,
        Eyes,
        Mask,
        Armour,
        Parachutes,
        Accessories,
        Items,
        Decals,
        Collars,
        // props
        Hats,
        Glasses,
        Earrings,
        Watches,
        Bangles,
        // head overlays
        Blemishes,
        FacialHair,
        Eyebrows,
        Ageing,
        Makeup,
        Blush,
        Complexion,
        SunDamage,
        Lipstick,
        Moles,
        ChestHair,
        BodyBlemishes,
        AddBodyBlemishes,
    }

    public readonly Dictionary<SlotKeyName, SlotKey> sp_slots = new Dictionary<SlotKeyName, SlotKey>
    {
        [SlotKeyName.Face] = new SlotKey(SlotType.CompVar, 0),
        [SlotKeyName.Beard] = new SlotKey(SlotType.CompVar, 1),
        [SlotKeyName.Haircut] = new SlotKey(SlotType.CompVar, 2),
        [SlotKeyName.Shirt] = new SlotKey(SlotType.CompVar, 3),
        [SlotKeyName.Pants] = new SlotKey(SlotType.CompVar, 4),
        [SlotKeyName.Hands] = new SlotKey(SlotType.CompVar, 5),
        [SlotKeyName.Shoes] = new SlotKey(SlotType.CompVar, 6),
        [SlotKeyName.Eyes] = new SlotKey(SlotType.CompVar, 7),
        [SlotKeyName.Accessories] = new SlotKey(SlotType.CompVar, 8),
        [SlotKeyName.Items] = new SlotKey(SlotType.CompVar, 9),
        [SlotKeyName.Decals] = new SlotKey(SlotType.CompVar, 10),
        [SlotKeyName.Collars] = new SlotKey(SlotType.CompVar, 11),
        [SlotKeyName.Hats] = new SlotKey(SlotType.Prop, 0),
        [SlotKeyName.Glasses] = new SlotKey(SlotType.Prop, 1),
        [SlotKeyName.Earrings] = new SlotKey(SlotType.Prop, 2),
        [SlotKeyName.Watches] = new SlotKey(SlotType.Prop, 6),
        [SlotKeyName.Bangles] = new SlotKey(SlotType.Prop, 7),
    };

    public readonly Dictionary<SlotKeyName, SlotKey> mp_slots = new Dictionary<SlotKeyName, SlotKey>
    {
        [SlotKeyName.Face] = new SlotKey(SlotType.CompVar, 0),
        [SlotKeyName.Mask] = new SlotKey(SlotType.CompVar, 1),
        [SlotKeyName.Haircut] = new SlotKey(SlotType.CompVar, 2),
        [SlotKeyName.Hands] = new SlotKey(SlotType.CompVar, 3),
        [SlotKeyName.Pants] = new SlotKey(SlotType.CompVar, 4),
        [SlotKeyName.Parachutes] = new SlotKey(SlotType.CompVar, 5),
        [SlotKeyName.Shoes] = new SlotKey(SlotType.CompVar, 6),
        [SlotKeyName.Accessories] = new SlotKey(SlotType.CompVar, 7),
        [SlotKeyName.SubShirt] = new SlotKey(SlotType.CompVar, 8),
        [SlotKeyName.Armour] = new SlotKey(SlotType.CompVar, 9),
        [SlotKeyName.Decals] = new SlotKey(SlotType.CompVar, 10),
        [SlotKeyName.Shirt] = new SlotKey(SlotType.CompVar, 11),
        [SlotKeyName.Hats] = new SlotKey(SlotType.Prop, 0),
        [SlotKeyName.Glasses] = new SlotKey(SlotType.Prop, 1),
        [SlotKeyName.Earrings] = new SlotKey(SlotType.Prop, 2),
        [SlotKeyName.Watches] = new SlotKey(SlotType.Prop, 6),
        [SlotKeyName.Bangles] = new SlotKey(SlotType.Prop, 7),
        [SlotKeyName.Blemishes] = new SlotKey(SlotType.HeadOverlay, 0),
        [SlotKeyName.FacialHair] = new SlotKey(SlotType.HeadOverlay, 1),
        [SlotKeyName.Eyebrows] = new SlotKey(SlotType.HeadOverlay, 2),
        [SlotKeyName.Ageing] = new SlotKey(SlotType.HeadOverlay, 3),
        [SlotKeyName.Makeup] = new SlotKey(SlotType.HeadOverlay, 4),
        [SlotKeyName.Blush] = new SlotKey(SlotType.HeadOverlay, 5),
        [SlotKeyName.Complexion] = new SlotKey(SlotType.HeadOverlay, 6),
        [SlotKeyName.SunDamage] = new SlotKey(SlotType.HeadOverlay, 7),
        [SlotKeyName.Lipstick] = new SlotKey(SlotType.HeadOverlay, 8),
        [SlotKeyName.Moles] = new SlotKey(SlotType.HeadOverlay, 9),
        [SlotKeyName.ChestHair] = new SlotKey(SlotType.HeadOverlay, 10),
        [SlotKeyName.BodyBlemishes] = new SlotKey(SlotType.HeadOverlay, 11),
        [SlotKeyName.AddBodyBlemishes] = new SlotKey(SlotType.HeadOverlay, 12),
    };

    // map slot type and slot id to drawable, texture, and palette
    public PedData ped_data = new PedData();

    public UIMenu AddSubMenu(UIMenu menu, string name)
    {
        return AddSubMenu2(menu, name).Item2;
    }

    public Tuple<UIMenuItem, UIMenu> AddSubMenu2(UIMenu menu, string name)
    {
        var item = new UIMenuItem(name);
        menu.AddItem(item);
        var submenu = new UIMenu(menu.Title.Caption, name);
        menuPool.Add(submenu);
        menu.BindMenuToItem(submenu, item);
        return Tuple.Create(item, submenu);
    }

    public void RefreshIndex()
    {
        foreach (UIMenu menu in menuPool.ToList()) menu.RefreshIndex();
    }

    public void AddStorymodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Story Mode");
        AddStorymodeModelToMenu(submenu);
        AddChangeOutfitToMenu(submenu, sp_slots);
        AddClearOutfitToMenu(submenu);
    }

    public void AddStorymodeModelToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Change Model");
        foreach (var pedItem in ChangingRoomPeds.peds)
            AddCategoryToMenu(submenu, pedItem.Item1, pedItem.Item2);
    }

    public void AddCategoryToMenu(UIMenu menu, string name, PedHash[] models)
    {
        var submenu = AddSubMenu(menu, name);
        foreach (PedHash model in models)
        {
            var subitem = new UIMenuItem(model.ToString());
            submenu.AddItem(subitem);
        }
        submenu.OnItemSelect += (sender, item, index) =>
        {
            ped_data.ChangePlayerModel(_pedhash[item.Text]);
        };
    }

    public void AddChangeOutfitToMenu(UIMenu menu, Dictionary<SlotKeyName, SlotKey> slot_map)
    {
        var result = AddSubMenu2(menu, "Change Outfit");
        var outfititem = result.Item1;
        var outfitmenu = result.Item2;
        var slotitems = new List<Tuple<SlotKeyName, UIMenuItem>>();
        foreach (SlotKeyName slot in Enum.GetValues(typeof(SlotKeyName)))
            if (slot_map.ContainsKey(slot))
            {
                var subitem = AddSlotToMenu(outfitmenu, slot.ToString(), slot_map[slot]);
                slotitems.Add(Tuple.Create(slot, subitem));
            }
        menu.OnItemSelect += (sender, item, index) =>
        {
            // enable only if there are any items to change
            if (item == outfititem)
            {
                foreach (var slotitem in slotitems)
                {
                    var slot_key_name = slotitem.Item1;
                    var subitem = slotitem.Item2;
                    var slot_key = slot_map[slot_key_name];
                    var ped = Game.Player.Character;
                    subitem.Enabled = (PedData.GetNumIndex1(ped, slot_key) >= 2) || (PedData.GetNumIndex2(ped, slot_key, 0) >= 2);
                }
            }
        };
    }

    public UIMenuItem AddSlotToMenu(UIMenu menu, string text, SlotKey slot_key)
    {
        var result = AddSubMenu2(menu, text);
        var subitem = result.Item1;
        var submenu = result.Item2;
        var listitem1 = new UIMenuListItem("Model", UI_LIST, 0);
        var listitem2 = new UIMenuListItem("Texture", UI_LIST, 0);
        var clearitem = new UIMenuItem("Clear");
        submenu.AddItem(listitem1);
        submenu.AddItem(listitem2);
        submenu.AddItem(clearitem);
        menu.OnItemSelect += (sender, item, index) =>
        {
            if (item == subitem)
            {
                // display correct indices for model and texture
                // and enable item if there's anything to change
                var ped = Game.Player.Character;
                var slot_value = ped_data.GetSlotValue(slot_key);
                listitem1.Index = slot_value.index1;
                listitem1.Enabled = (PedData.GetNumIndex1(ped, slot_key) >= 2);
                listitem2.Index = slot_value.index2;
                listitem2.Enabled = (ped_data.GetNumIndex2(ped, slot_key) >= 2);
            }
        };
        submenu.OnListChange += (sender, item, index) =>
        {
            if (item == listitem1 || item == listitem2)
            {
                var ped = Game.Player.Character;
                var slot_value = ped_data.GetSlotValue(slot_key);
                var numIndex1 = PedData.GetNumIndex1(ped, slot_key);
                var numIndex2 = ped_data.GetNumIndex2(ped, slot_key);
                // we need to ensure that the new id is valid as the menu has more items than number of ids supported by the game
                var maxid = Math.Min(UI_LIST_MAX, ((item == listitem1) ? numIndex1 : numIndex2)) - 1;
                System.Diagnostics.Debug.Assert(maxid >= 0);
                System.Diagnostics.Debug.Assert(maxid <= UI_LIST_MAX - 1);
                if (index > maxid)
                {
                    // wrap the index depending on whether user scrolled forward or backward
                    index = (index == UI_LIST_MAX - 1) ? maxid : 0;
                    item.Index = index;
                }
                var newNumIndex2 = numIndex2;  // new numIndex2 after changing index1 (if changed)
                if (item == listitem1)
                {
                    slot_value.index1 = index;
                    newNumIndex2 = PedData.GetNumIndex2(ped, slot_key, slot_value.index1);
                    // correct current index2 if it is out of range
                    // we pick the nearest integer
                    if (slot_value.index2 >= newNumIndex2) slot_value.index2 = newNumIndex2 - 1;
                    // update listitem2 index and enabled flag
                    listitem2.Index = slot_value.index2;
                    listitem2.Enabled = (newNumIndex2 >= 2);
                }
                else
                {
                    slot_value.index2 = index;
                }
                ped_data.SetSlotValue(ped, slot_key, slot_value);
            }
        };
        submenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == clearitem)
            {
                var ped = Game.Player.Character;
                ped_data.ClearSlotValue(ped, slot_key);
                // update menu items
                var slot_value = ped_data.GetSlotValue(slot_key);
                listitem1.Index = slot_value.index1;
                listitem2.Index = slot_value.index2;
                listitem2.Enabled = (PedData.GetNumIndex2(ped, slot_key, slot_value.index1) >= 2);
            }
        };
        return subitem;
    }

    public void AddFreemodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Free Mode");
        AddFreemodeModelToMenu(submenu);
        AddChangeOutfitToMenu(submenu, mp_slots);
        AddClearOutfitToMenu(submenu);
    }

    public void AddFreemodeModelToMenu(UIMenu menu)
    {
        var male = new UIMenuItem("Male");
        var female = new UIMenuItem("Female");
        menu.AddItem(male);
        menu.AddItem(female);
        menu.OnItemSelect += (sender, item, index) =>
        {
            if (item == male)
                ped_data.ChangePlayerModel(PedHash.FreemodeMale01);
            else if (item == female)
                ped_data.ChangePlayerModel(PedHash.FreemodeFemale01);
        };
    }

    public bool IsPedFreemode(Ped ped)
    {
        var hash = (PedHash)ped.Model.Hash;
        return hash == PedHash.FreemodeMale01 || hash == PedHash.FreemodeFemale01;
    }

    public void AddClearOutfitToMenu(UIMenu menu)
    {
        var clearitem = new UIMenuItem("Clear Outfit");
        menu.AddItem(clearitem);
        menu.OnItemSelect += (sender, item, index) =>
        {
            if (item == clearitem)
            {
                var ped = Game.Player.Character;
                foreach (SlotType slot_type in Enum.GetValues(typeof(SlotType)))
                    for (int slot_id=0; slot_id < PedData.GetNumId(slot_type); slot_id++)
                    {
                        var slot_key = new SlotKey(slot_type, slot_id);
                        ped_data.ClearSlotValue(ped, slot_key);
                    }
            }
        };
    }

    public void AddActorActionToMenu(UIMenu menu, String name, Action<UIMenuItem, int> action, Func<int, bool> ticked)
    {
        var submenu = AddSubMenu(menu, name);
        for (int i = 0; i < 10; i++)
        {
            var subsubmenu = AddSubMenu(submenu, String.Format("Actors {0}-{1}", 1 + i * 10, 10 + i * 10));
            for (int j = 0; j < 10; j++)
            {
                var slot = 1 + j + i * 10;
                var slotitem = new UIMenuItem(String.Format("Actor {0}", slot));
                subsubmenu.AddItem(slotitem);
                if (ticked(slot)) slotitem.SetLeftBadge(UIMenuItem.BadgeStyle.Tick);
                subsubmenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == slotitem) action(slotitem, slot);
                };
            }
        }
    }

    public String GetScriptFolder()
    {
        return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChangingRoom");
    }

    public String GetActorFilename(int slot)
    {
        var filename = String.Format("actor{0:000}.xml", slot);
        return System.IO.Path.Combine(GetScriptFolder(), filename);
    }

    public bool ExistsActor(int slot)
    {
        var path = GetActorFilename(slot);
        return System.IO.File.Exists(path);
    }

    public void SaveActor(UIMenuItem item, int slot)
    {
        var path = GetActorFilename(slot);
        UI.Notify(String.Format("Saving actor to {0}", path));
        var settings = new XmlWriterSettings();
        settings.Indent = true;
        settings.NewLineOnAttributes = true;
        using (XmlWriter writer = XmlWriter.Create(path, settings))
        {
            writer.WriteStartElement("GtaVNative");
            {
                var name = ((PedHash)Game.Player.Character.Model.Hash).ToString();
                writer.WriteStartElement("SetPlayerModel");
                writer.WriteAttributeString("name", name);
                writer.WriteEndElement();
            }
            ped_data.WriteXml(writer);
            writer.WriteEndElement();
        }
    }

    public void LoadActor(UIMenuItem item, int slot)
    {
        var path = GetActorFilename(slot);
        UI.Notify(String.Format("Loading actor from {0}", path));
        var reader = new XmlTextReader(path);
        while (reader.Read())
        {
            if (reader.Name == "SetPlayerModel")
            {
                ped_data.ChangePlayerModel(_pedhash[reader.GetAttribute("name")]);
            }
            else if (reader.Name == "SetSlotValue")
            {
                var key = new SlotKey(reader);
                var val = new SlotValue(reader);
                ped_data.SetSlotValue(Game.Player.Character, key, val);
            }
        }
    }

    public ChangingRoom()
    {
        menuPool = new MenuPool();
        var menuMain = new UIMenu("Changing Room", "Main Menu");
        menuPool.Add(menuMain);
        AddStorymodeToMenu(menuMain);
        AddFreemodeToMenu(menuMain);
        AddActorActionToMenu(menuMain, "Save Actor", SaveActor, ExistsActor);
        AddActorActionToMenu(menuMain, "Load Actor", LoadActor, ExistsActor);
        RefreshIndex();

        Tick += (sender, e) => menuPool.ProcessMenus();
        KeyUp += (sender, e) =>
        {
            if (e.KeyCode == Keys.F5 && !menuPool.IsAnyMenuOpen())
                menuMain.Visible = !menuMain.Visible;
        };

        _pedhash = new Dictionary<string, PedHash>();
        foreach (PedHash x in Enum.GetValues(typeof(PedHash))) _pedhash[x.ToString()] = x;
        var path = GetScriptFolder();
        if (!System.IO.Directory.Exists(path))
        {
            UI.Notify("Creating directory " + path);
            System.IO.Directory.CreateDirectory(path);
        }
    }
}
