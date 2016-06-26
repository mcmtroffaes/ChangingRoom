/*
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
    Eye,
    Parent,
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
    public int index1, index2, index3, index4;

    public SlotValue(int i1, int i2, int i3, int i4)
    {
        index1 = i1;
        index2 = i2;
        index3 = i3;
        index4 = i4;
    }

    public SlotValue(XmlTextReader reader)
    {
        index1 = int.Parse(reader.GetAttribute("index1"));
        index2 = int.Parse(reader.GetAttribute("index2"));
        index3 = int.Parse(reader.GetAttribute("index3"));
        index4 = int.Parse(reader.GetAttribute("index4"));
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("index1", index1.ToString());
        writer.WriteAttributeString("index2", index2.ToString());
        writer.WriteAttributeString("index3", index3.ToString());
        writer.WriteAttributeString("index4", index4.ToString());
    }
}

public class PedData
{
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

    private readonly static int[] mp_male_clear_drawable =
    {
        0, 0, 0, 3, 11, 0, 13, 0, 15, 0, 0, 15
    };

    private readonly static int[] mp_female_clear_drawable =
    {
        0, 0, 0, 8, 13, 0, 12, 0, 14, 0, 0, 82
    };

    private readonly static int[] mp_male_undress_drawable =
    {
        0, 0, 0, 15, 61, 0, 34, 0, 15, 0, 0, 15
    };

    private readonly static int[] mp_female_undress_drawable =
    {
        0, 0, 0, 15, 15, 0, 35, 0, 2, 0, 0, 15
    };

    private enum ColorType
    {
        None,
        Hair,
        Makeup,
    };

    private readonly static ColorType[] mp_head_overlay_color_type =
    {
        ColorType.None,
        ColorType.Hair, // 1 = facial hair
        ColorType.Hair, // 2 = eyebrows
        ColorType.None,
        ColorType.None,
        ColorType.Makeup, // 5 = cheek blush
        ColorType.None,
        ColorType.None,
        ColorType.Makeup, // 8 = lipstick
        ColorType.None,
        ColorType.Hair, // 10 = chest hair
        ColorType.None,
        ColorType.None,
    };

    public SlotValue GetSlotValue(SlotKey slot)
    {
        if (data.ContainsKey(slot))
            return data[slot];
        else
            return new SlotValue(0, 0, 0, 0);
    }

    public static int GetNumId(SlotType typ)
    {
        switch (typ)
        {
            case SlotType.CompVar: return 12;
            case SlotType.Prop: return 8;
            case SlotType.HeadOverlay: return 13;
            default: return 1;
        }
    }

    public static string[] GetIndexNames(SlotType typ)
    {
        switch (typ)
        {
            case SlotType.CompVar: return new string[] { "Model", "Texture", "Color", "Highlight Color" };
            case SlotType.Prop: return new string[] { "Model", "Texture", "Color", "Highlight Color" };
            case SlotType.HeadOverlay: return new string[] { "Model", "Opacity", "Color", "Highlight Color" };
            case SlotType.Parent: return new string[] { "Mom", "Dad", "Resemblance", "Skin Tone" };
            default: return new string[] { "Model", "Texture", "Color", "Highlight Color" };
        }
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
            case SlotType.Eye:
                // TODO: hardcoded for now; how to get this from native?
                return 32;
            case SlotType.Parent:
                return (
                    Function.Call<int>(Hash._GET_NUM_PARENT_PEDS_OF_TYPE, 0) +
                    Function.Call<int>(Hash._GET_NUM_PARENT_PEDS_OF_TYPE, 1) +
                    Function.Call<int>(Hash._GET_NUM_PARENT_PEDS_OF_TYPE, 2) +
                    Function.Call<int>(Hash._GET_NUM_PARENT_PEDS_OF_TYPE, 3)
                    );
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
            case SlotType.Parent:
                return (
                    Function.Call<int>(Hash._GET_NUM_PARENT_PEDS_OF_TYPE, 0) +
                    Function.Call<int>(Hash._GET_NUM_PARENT_PEDS_OF_TYPE, 1) +
                    Function.Call<int>(Hash._GET_NUM_PARENT_PEDS_OF_TYPE, 2) +
                    Function.Call<int>(Hash._GET_NUM_PARENT_PEDS_OF_TYPE, 3)
                    );
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
        if (key.typ == SlotType.CompVar && key.id == 2) // hair
        {
            return Function.Call<int>(Hash._GET_NUM_HAIR_COLORS);
        }
        else if (key.typ == SlotType.HeadOverlay && index1 >= 1)
        {
            ColorType color_type = mp_head_overlay_color_type[key.id];
            switch (color_type)
            {
                case ColorType.Hair:
                    return Function.Call<int>(Hash._GET_NUM_HAIR_COLORS);
                case ColorType.Makeup:
                    // _GET_NUM_MAKEUP_COLORS
                    return Function.Call<int>(Hash._0xD1F7CA1535D22818);
            }
        }
        else if (key.typ == SlotType.Parent)
            return 9; // resemblance
        return 1;
    }

    public int GetNumIndex4(Ped ped, SlotKey key)
    {
        var slot_value = GetSlotValue(key);
        return GetNumIndex4(ped, key, slot_value.index1, slot_value.index2, slot_value.index3);
    }

    public static int GetNumIndex4(Ped ped, SlotKey key, int index1, int index2, int index3)
    {
        // hair: highlight color
        if (key.typ == SlotType.CompVar && key.id == 2)
            return Function.Call<int>(Hash._GET_NUM_HAIR_COLORS);
        else if (key.typ == SlotType.Parent)
            return 9; // skin tone
        else
            return 1;
    }

    public void SetSlotValue(Ped ped, SlotKey slot_key, SlotValue slot_value)
    {
        // store it
        if (slot_value.index1 == 0 && slot_value.index2 == 0 && slot_value.index3 == 0 && slot_value.index4 == 0)
            data.Remove(slot_key);
        else
            data[slot_key] = slot_value;
        // change game state accordingly
        switch (slot_key.typ)
        {
            case SlotType.CompVar:
                Function.Call(
                    Hash.SET_PED_COMPONENT_VARIATION,
                    ped.Handle,
                    slot_key.id, slot_value.index1, slot_value.index2, 0);
                if (slot_key.id == 2) // hair: use index3 and index4
                    Function.Call(Hash._SET_PED_HAIR_COLOR, ped.Handle, slot_value.index3, slot_value.index4);
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
                {
                    // index2 is opacity
                    Function.Call(Hash.SET_PED_HEAD_OVERLAY, ped.Handle, slot_key.id, slot_value.index1 - 1, (8 - slot_value.index2) / 8.0f);
                    // takes color (index3) and highlight color (index4), but the latter is ignored
                    // so use index3 for both colors just in case
                    var color_type = mp_head_overlay_color_type[slot_key.id];
                    switch (color_type)
                    {
                        case ColorType.Hair:
                            Function.Call(Hash._SET_PED_HEAD_OVERLAY_COLOR, ped.Handle, slot_key.id, 1, slot_value.index3, slot_value.index3);
                            break;
                        case ColorType.Makeup:
                            Function.Call(Hash._SET_PED_HEAD_OVERLAY_COLOR, ped.Handle, slot_key.id, 2, slot_value.index3, slot_value.index3);
                            break;
                    }
                }
                break;
            case SlotType.Eye:
                Function.Call(Hash._SET_PED_EYE_COLOR, ped.Handle, slot_value.index1);
                break;
            case SlotType.Parent:
                // get current parent info
                var par = GetSlotValue(new SlotKey(SlotType.Parent, 0));
                var shape1 = par.index1;
                var shape2 = par.index2;
                var skin1 = par.index1;
                var skin2 = par.index2;
                float shapemix = par.index3 / 8.0f;
                float skinmix = par.index4 / 8.0f;
                Function.Call(
                    Hash.SET_PED_HEAD_BLEND_DATA, ped.Handle,
                    shape1, shape2, 0, skin1, skin2, 0, shapemix, skinmix, 0.0f);
                break;
        }
    }

    public void ClearSlot(Ped ped, SlotKey slot_key)
    {
        var slot_value = new SlotValue(0, 0, 0, 0);
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

    public void FreemodeUndressSlot(Ped ped, SlotKey slot_key)
    {
        var slot_value = new SlotValue(0, 0, 0, 0);
        if (slot_key.typ == SlotType.CompVar)
        {
            var hash = (PedHash)ped.Model.Hash;
            if (hash == PedHash.FreemodeMale01)
                slot_value.index1 = mp_male_undress_drawable[slot_key.id];
            else if (hash == PedHash.FreemodeFemale01)
                slot_value.index1 = mp_female_undress_drawable[slot_key.id];
        }
        SetSlotValue(ped, slot_key, slot_value);
    }

    // Undress but keep all head overlays. Returns dictionary that can be used to redress.
    public Dictionary<SlotKey, SlotValue> FreemodeUndress(Ped ped)
    {
        var old_data = new Dictionary<SlotKey, SlotValue>();
        old_data.Clear();
        SlotType[] typs = { SlotType.CompVar, SlotType.Prop };
        foreach (SlotType slot_type in typs)
            for (int slot_id = 0; slot_id < PedData.GetNumId(slot_type); slot_id++)
            {
                if (slot_id == 2) continue; // don't "undress" hair
                var slot_key = new SlotKey(slot_type, slot_id);
                old_data[slot_key] = GetSlotValue(slot_key);
                FreemodeUndressSlot(ped, slot_key);
            }
        return old_data;
    }

    public void FreemodeRedress(Ped ped, Dictionary<SlotKey, SlotValue> old_data)
    {
        foreach (var slot_item in old_data)
            SetSlotValue(ped, slot_item.Key, slot_item.Value);
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
            {
                // must call SET_PED_HEAD_BLEND_DATA otherwise head overlays don't work
                var slot_key = new SlotKey(SlotType.Parent, 0);
                var slot_value = new SlotValue(0, 0, 4, 4);
                var ped = Game.Player.Character;
                SetSlotValue(ped, slot_key, slot_value);
            }
            data.Clear();
        }
        model.MarkAsNoLongerNeeded();
    }
}

public class ChangingRoom : Script
{
    private Random rnd = new Random();
    static int UI_LIST_MAX = 256;
    static List<dynamic> UI_LIST = Enumerable.Range(0, UI_LIST_MAX).Cast<dynamic>().ToList();
    private readonly Dictionary<string, PedHash> _pedhash;
    private MenuPool menuPool;

    // map slot type and slot id to drawable, texture, and palette
    public PedData ped_data = new PedData();

    public UIMenu AddSubMenu(UIMenu menu, string name)
    {
        var item = new UIMenuItem(name);
        menu.AddItem(item);
        var submenu = new UIMenu(menu.Title.Caption, name);
        menuPool.Add(submenu);
        menu.BindMenuToItem(submenu, item);
        return submenu;
    }

    public void RefreshIndex()
    {
        foreach (UIMenu menu in menuPool.ToList()) menu.RefreshIndex();
    }

    public void AddStorymodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Story Mode");
        AddStorymodeModelToMenu(submenu);
        AddStorymodeAppearanceToMenu(submenu);
        AddClearAppearanceToMenu(submenu);
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

    public void AddStorymodeAppearanceToMenu(UIMenu menu)
    {
        var topmenu = AddSubMenu(menu, "Appearance");
        var charmenu = AddSubMenu(topmenu, "Character");
        var barbmenu = AddSubMenu(topmenu, "Barber");
        var clo1menu = AddSubMenu(topmenu, "Clothing");
        var clo2menu = AddSubMenu(topmenu, "Clothing Extra");
        AddSlotToMenu(charmenu, "Face", new SlotKey(SlotType.CompVar, 0));
        AddSlotToMenu(charmenu, "Eyes", new SlotKey(SlotType.CompVar, 7));
        AddSlotToMenu(barbmenu, "Haircut", new SlotKey(SlotType.CompVar, 2));
        AddSlotToMenu(barbmenu, "Beard", new SlotKey(SlotType.CompVar, 1));
        AddSlotToMenu(clo1menu, "Hands", new SlotKey(SlotType.CompVar, 5));
        AddSlotToMenu(clo1menu, "Shirt", new SlotKey(SlotType.CompVar, 3));
        AddSlotToMenu(clo1menu, "Pants", new SlotKey(SlotType.CompVar, 4));
        AddSlotToMenu(clo1menu, "Shoes", new SlotKey(SlotType.CompVar, 6));
        AddSlotToMenu(clo2menu, "Hat", new SlotKey(SlotType.Prop, 0));
        AddSlotToMenu(clo2menu, "Glasses", new SlotKey(SlotType.Prop, 1));
        AddSlotToMenu(clo2menu, "Earrings", new SlotKey(SlotType.Prop, 2));
        AddSlotToMenu(clo2menu, "Watch", new SlotKey(SlotType.Prop, 6));
        AddSlotToMenu(clo2menu, "Bangle", new SlotKey(SlotType.Prop, 7));
        AddSlotToMenu(clo2menu, "Accessory", new SlotKey(SlotType.CompVar, 8));
        AddSlotToMenu(clo2menu, "Item", new SlotKey(SlotType.CompVar, 9));
        AddSlotToMenu(clo2menu, "Decal", new SlotKey(SlotType.CompVar, 10));
        AddSlotToMenu(clo2menu, "Collar", new SlotKey(SlotType.CompVar, 11));
    }

    public void AddFreemodeAppearanceToMenu(UIMenu menu)
    {
        var topmenu = AddSubMenu(menu, "Appearance");
        var charmenu = AddSubMenu(topmenu, "Character");
        var barbmenu = AddSubMenu(topmenu, "Barber");
        var clo1menu = AddSubMenu(topmenu, "Clothing");
        var clo2menu = AddSubMenu(topmenu, "Clothing Extra");
        // we use parent blend to change face
        //AddSlotToMenu(charmenu, "Face", new SlotKey(SlotType.CompVar, 0));
        AddSlotToMenu(barbmenu, "Haircut", new SlotKey(SlotType.CompVar, 2));
        AddSlotToMenu(barbmenu, "Beard", new SlotKey(SlotType.HeadOverlay, 1));
        AddSlotToMenu(barbmenu, "Eyebrows", new SlotKey(SlotType.HeadOverlay, 2));
        AddSlotToMenu(barbmenu, "Makeup", new SlotKey(SlotType.HeadOverlay, 4));
        AddSlotToMenu(barbmenu, "Blush", new SlotKey(SlotType.HeadOverlay, 5));
        AddSlotToMenu(barbmenu, "Lipstick", new SlotKey(SlotType.HeadOverlay, 8));
        AddSlotToMenu(barbmenu, "Chest Hair", new SlotKey(SlotType.HeadOverlay, 10));
        AddSlotToMenu(barbmenu, "Eyes", new SlotKey(SlotType.Eye, 0));
        AddSlotToMenu(charmenu, "Heritage", new SlotKey(SlotType.Parent, 0));
        AddSlotToMenu(charmenu, "Blemishes", new SlotKey(SlotType.HeadOverlay, 0));
        AddSlotToMenu(charmenu, "Ageing", new SlotKey(SlotType.HeadOverlay, 3));
        AddSlotToMenu(charmenu, "Complexion", new SlotKey(SlotType.HeadOverlay, 6));
        AddSlotToMenu(charmenu, "Sun Damage", new SlotKey(SlotType.HeadOverlay, 7));
        AddSlotToMenu(charmenu, "Moles", new SlotKey(SlotType.HeadOverlay, 9));
        AddSlotToMenu(charmenu, "Body Blemishes", new SlotKey(SlotType.HeadOverlay, 11));
        AddSlotToMenu(charmenu, "Add Body Blemishes", new SlotKey(SlotType.HeadOverlay, 12));
        AddSlotToMenu(clo1menu, "Hands", new SlotKey(SlotType.CompVar, 3));
        AddSlotToMenu(clo1menu, "Shirt", new SlotKey(SlotType.CompVar, 11));
        AddSlotToMenu(clo1menu, "Extra Shirt", new SlotKey(SlotType.CompVar, 8));
        AddSlotToMenu(clo1menu, "Pants", new SlotKey(SlotType.CompVar, 4));
        AddSlotToMenu(clo1menu, "Shoes", new SlotKey(SlotType.CompVar, 6));
        AddSlotToMenu(clo2menu, "Hat", new SlotKey(SlotType.Prop, 0));
        AddSlotToMenu(clo2menu, "Mask", new SlotKey(SlotType.CompVar, 1));
        AddSlotToMenu(clo2menu, "Glasses", new SlotKey(SlotType.Prop, 1));
        AddSlotToMenu(clo2menu, "Earrings", new SlotKey(SlotType.Prop, 2));
        AddSlotToMenu(clo2menu, "Tie & Scarf", new SlotKey(SlotType.CompVar, 7));
        AddSlotToMenu(clo2menu, "Watch", new SlotKey(SlotType.Prop, 6));
        AddSlotToMenu(clo2menu, "Bangle", new SlotKey(SlotType.Prop, 7));
        AddSlotToMenu(clo2menu, "Parachute", new SlotKey(SlotType.CompVar, 5));
        AddSlotToMenu(clo2menu, "Armour", new SlotKey(SlotType.CompVar, 9));
        AddSlotToMenu(clo2menu, "Decal", new SlotKey(SlotType.CompVar, 10));
        // undress character when Character or Barber menus are openend
        var old_data = new Dictionary<SlotKey, SlotValue>();
        charmenu.ParentMenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == charmenu.ParentItem || item == barbmenu.ParentItem)
            {
                var ped = Game.Player.Character;
                old_data = ped_data.FreemodeUndress(ped);
            }
        };
        // redress character when the menu is closed
        charmenu.OnMenuClose += (sender) =>
        {
            var ped = Game.Player.Character;
            ped_data.FreemodeRedress(ped, old_data);
            old_data.Clear();
        };
        barbmenu.OnMenuClose += (sender) =>
        {
            var ped = Game.Player.Character;
            ped_data.FreemodeRedress(ped, old_data);
            old_data.Clear();
        };
    }

    public void AddSlotToMenu(UIMenu menu, string text, SlotKey slot_key)
    {
        var submenu = AddSubMenu(menu, text);
        var index_names = PedData.GetIndexNames(slot_key.typ);
        var listitem1 = new UIMenuListItem(index_names[0], UI_LIST, 0);
        var listitem2 = new UIMenuListItem(index_names[1], UI_LIST, 0);
        var listitem3 = new UIMenuListItem(index_names[2], UI_LIST, 0);
        var listitem4 = new UIMenuListItem(index_names[3], UI_LIST, 0);
        var randomitem = new UIMenuItem("Random");
        var clearitem = new UIMenuItem("Clear");
        submenu.AddItem(listitem1);
        submenu.AddItem(listitem2);
        submenu.AddItem(listitem3);
        submenu.AddItem(listitem4);
        submenu.AddItem(randomitem);
        submenu.AddItem(clearitem);
        // when the menu is selected, we only want submenu to be
        // enabled if its model and/or texture can be changed
        menu.ParentMenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == menu.ParentItem)
            {
                var ped = Game.Player.Character;
                submenu.ParentItem.Enabled = (
                    (PedData.GetNumIndex1(ped, slot_key) >= 2) ||
                    (PedData.GetNumIndex2(ped, slot_key, 0) >= 2) ||
                    (PedData.GetNumIndex3(ped, slot_key, 0, 0) >= 2) ||
                    (PedData.GetNumIndex4(ped, slot_key, 0, 0, 0) >= 2));
            }
        };
        // when submenu is selected, display correct indices for model and texture
        // and enable those entries of this submenu
        // (model, texture, ...) where something can be changed
        submenu.ParentMenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == submenu.ParentItem)
            {
                var ped = Game.Player.Character;
                var slot_value = ped_data.GetSlotValue(slot_key);
                listitem1.Index = slot_value.index1;
                listitem1.Enabled = (PedData.GetNumIndex1(ped, slot_key) >= 2);
                listitem2.Index = slot_value.index2;
                listitem2.Enabled = (ped_data.GetNumIndex2(ped, slot_key) >= 2);
                listitem3.Index = slot_value.index3;
                listitem3.Enabled = (ped_data.GetNumIndex3(ped, slot_key) >= 2);
                listitem4.Index = slot_value.index4;
                listitem4.Enabled = (ped_data.GetNumIndex4(ped, slot_key) >= 2);
            }
        };
        submenu.OnListChange += (sender, item, index) =>
        {
            if (item == listitem1 || item == listitem2 || item == listitem3 || item == listitem4)
            {
                var ped = Game.Player.Character;
                var slot_value = ped_data.GetSlotValue(slot_key);
                var numIndex1 = PedData.GetNumIndex1(ped, slot_key);
                var numIndex2 = ped_data.GetNumIndex2(ped, slot_key);
                var numIndex3 = ped_data.GetNumIndex3(ped, slot_key);
                var numIndex4 = ped_data.GetNumIndex4(ped, slot_key);
                // we need to ensure that the new id is valid as the menu has more items than number of ids supported by the game
                int maxid;
                if (item == listitem1)
                    maxid = numIndex1;
                else if (item == listitem2)
                    maxid = numIndex2;
                else if (item == listitem3)
                    maxid = numIndex3;
                else // if (item == listitem4)
                    maxid = numIndex4;
                maxid = Math.Min(maxid - 1, UI_LIST_MAX);
                System.Diagnostics.Debug.Assert(maxid >= 0);
                System.Diagnostics.Debug.Assert(maxid <= UI_LIST_MAX - 1);
                if (index > maxid)
                {
                    // wrap the index depending on whether user scrolled forward or backward
                    index = (index == UI_LIST_MAX - 1) ? maxid : 0;
                    item.Index = index;
                }
                if (item == listitem1)
                    slot_value.index1 = index;
                else if (item == listitem2)
                    slot_value.index2 = index;
                else if (item == listitem3)
                    slot_value.index3 = index;
                else // if (item == listitem4)
                    slot_value.index4 = index;
                // correct listitem2 if index2 is out of range
                var newNumIndex2 = PedData.GetNumIndex2(ped, slot_key, slot_value.index1);
                if (slot_value.index2 >= newNumIndex2) slot_value.index2 = newNumIndex2 - 1;
                listitem2.Index = slot_value.index2;
                listitem2.Enabled = (newNumIndex2 >= 2);
                // correct listitem3 if index3 is out of range
                var newNumIndex3 = PedData.GetNumIndex3(ped, slot_key, slot_value.index1, slot_value.index2);
                if (slot_value.index3 >= newNumIndex3) slot_value.index3 = newNumIndex3 - 1;
                listitem3.Index = slot_value.index3;
                listitem3.Enabled = (newNumIndex3 >= 2);
                // correct listitem3 if index3 is out of range
                var newNumIndex4 = PedData.GetNumIndex4(ped, slot_key, slot_value.index1, slot_value.index2, slot_value.index3);
                if (slot_value.index4 >= newNumIndex4) slot_value.index4 = newNumIndex4 - 1;
                listitem4.Index = slot_value.index4;
                listitem4.Enabled = (newNumIndex4 >= 2);
                // set slot value
                ped_data.SetSlotValue(ped, slot_key, slot_value);
            }
        };
        submenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == clearitem)
            {
                var ped = Game.Player.Character;
                ped_data.ClearSlot(ped, slot_key);
                // update menu items
                var slot_value = ped_data.GetSlotValue(slot_key);
                listitem1.Index = slot_value.index1;
                listitem2.Index = slot_value.index2;
                listitem3.Index = slot_value.index3;
                listitem4.Index = slot_value.index4;
                listitem1.Enabled = (PedData.GetNumIndex1(ped, slot_key) >= 2);
                listitem2.Enabled = (ped_data.GetNumIndex2(ped, slot_key) >= 2);
                listitem3.Enabled = (ped_data.GetNumIndex3(ped, slot_key) >= 2);
                listitem4.Enabled = (ped_data.GetNumIndex4(ped, slot_key) >= 2);
            }
            else if (item == randomitem)
            {
                var ped = Game.Player.Character;
                var slot_value = ped_data.GetSlotValue(slot_key);
                slot_value.index1 = rnd.Next(PedData.GetNumIndex1(ped, slot_key));
                slot_value.index2 = rnd.Next(PedData.GetNumIndex2(ped, slot_key, slot_value.index1));
                slot_value.index3 = rnd.Next(PedData.GetNumIndex3(ped, slot_key, slot_value.index1, slot_value.index2));
                slot_value.index4 = rnd.Next(PedData.GetNumIndex4(ped, slot_key, slot_value.index1, slot_value.index2, slot_value.index3));
                ped_data.SetSlotValue(ped, slot_key, slot_value);
                listitem1.Index = slot_value.index1;
                listitem2.Index = slot_value.index2;
                listitem3.Index = slot_value.index3;
                listitem4.Index = slot_value.index4;
                listitem1.Enabled = (PedData.GetNumIndex1(ped, slot_key) >= 2);
                listitem2.Enabled = (ped_data.GetNumIndex2(ped, slot_key) >= 2);
                listitem3.Enabled = (ped_data.GetNumIndex3(ped, slot_key) >= 2);
                listitem4.Enabled = (ped_data.GetNumIndex4(ped, slot_key) >= 2);
            }
        };
    }

    public void AddFreemodeToMenu(UIMenu menu)
    {
        var submenu = AddSubMenu(menu, "Free Mode");
        AddFreemodeModelToMenu(submenu);
        AddFreemodeAppearanceToMenu(submenu);
        AddClearAppearanceToMenu(submenu);
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

    public void AddClearAppearanceToMenu(UIMenu menu)
    {
        var clearitem = new UIMenuItem("Clear Appearance");
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
                        ped_data.ClearSlot(ped, slot_key);
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
