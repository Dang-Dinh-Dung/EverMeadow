using System;

namespace Evermeadow.Foundation.Input.Unity
{
    /// <summary>
    /// Maps core action ids to <c>InputAction</c> GUIDs in EvermeadowInput.inputactions (ADR-0001 Decision 6: ids are mapped by
    /// action GUID, never persisted). GUIDs are uuid5 of stable names, produced by the asset generator; the adapter validates
    /// the table against the asset at start-up.
    /// </summary>
    public static class ActionIds
    {
        /// <summary>GUID of the Move action (Gameplay map).</summary>
        public static readonly Guid Move = new Guid("ad4ffda3-2915-52ba-afc3-1a682e563918");

        /// <summary>GUID of the Navigate action (Menu map).</summary>
        public static readonly Guid Navigate = new Guid("74389c43-f7ef-52e1-bbde-8499e526faad");

        /// <summary>GUID of the Point action (Menu map; UI pointer only, not a core action).</summary>
        public static readonly Guid Point = new Guid("79bf5680-3cd0-57b0-a0cb-a4aa04c452f9");

        private static readonly Guid[] Buttons = Build();

        /// <summary>GUID of the button action <paramref name="id"/>, or <see cref="Guid.Empty"/>.</summary>
        public static Guid For(InputActionId id) =>
            id > InputActionId.None && id < InputActionId.Count ? Buttons[(int)id] : Guid.Empty;

        private static Guid[] Build()
        {
            var t = new Guid[(int)InputActionId.Count];
            t[(int)InputActionId.Jump] = new Guid("a046ba91-e932-520f-8011-94f552c4bad1");
            t[(int)InputActionId.Attack] = new Guid("2221238d-2647-5612-8a51-09a54cfd48ab");
            t[(int)InputActionId.Dodge] = new Guid("886ff14d-4ad1-59b9-8cb3-44d764d907f3");
            t[(int)InputActionId.Plunge] = new Guid("8b02389b-b352-593d-a0f1-b1eb32a1d682");
            t[(int)InputActionId.DropThrough] = new Guid("67129264-5ddd-5e5a-8c0b-d4b5781739e9");
            t[(int)InputActionId.Interact] = new Guid("646b612d-e5fa-58e0-a7ee-3323770d5998");
            t[(int)InputActionId.Heal] = new Guid("697736b2-ee18-5477-a886-59f02ca3b244");
            t[(int)InputActionId.EatBuff] = new Guid("8a59d51f-51a0-5b6c-a53b-da0ee5f1a073");
            t[(int)InputActionId.Map] = new Guid("5022fc11-885a-5cfb-ba1a-e8a71cc5e8e1");
            t[(int)InputActionId.Pause] = new Guid("99889478-12aa-5018-bd0e-1dbad3f0e2af");
            t[(int)InputActionId.Confirm] = new Guid("5dfa6daf-0831-5a73-a28a-dbc7a86ff7a7");
            t[(int)InputActionId.Cancel] = new Guid("f65710fd-1a89-57b7-9d9b-984f1aebe200");
            t[(int)InputActionId.TabLeft] = new Guid("4427a1ac-2672-5b21-a9fe-8b76af081e2e");
            t[(int)InputActionId.TabRight] = new Guid("b025b012-a028-5eb9-a52f-d102c2752d7e");
            t[(int)InputActionId.Click] = new Guid("0007e764-7c37-5588-b44e-ba134365cd2e");
            t[(int)InputActionId.Reel] = new Guid("552244fc-3b69-588e-9639-3eed5558cd92");
            t[(int)InputActionId.Run] = new Guid("b2cbed0b-8910-5d9b-b17e-3a926ac5ba2a"); // uuid5(URL, "evermeadow:input/Gameplay/Run")
            return t;
        }
    }
}
