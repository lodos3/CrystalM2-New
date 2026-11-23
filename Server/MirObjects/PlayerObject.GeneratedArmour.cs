using System;

namespace Server.MirObjects
{
    public partial class PlayerObject
    {
        // Session-only appearance override: disconnecting or using @arm ... off restores
        // the item-defined appearance. No DB migration or equipment mutation.
        private byte _generatedArmourVisual;
        protected override short AppearanceArmour =>
            GeneratedArmourVisual.Encode(Looks_Armour, _generatedArmourVisual);

        private void SetGeneratedArmourVisual(byte id)
        {
            if (id > GeneratedArmourVisual.MaxId)
                throw new ArgumentOutOfRangeException(nameof(id));
            _generatedArmourVisual = id;
            if (CurrentMap == null) return;

            // Nearby clients and the receiving player's own UserObject must both update.
            var update = GetUpdateInfo();
            Broadcast(update);
            Enqueue(update);
        }
    }
}
