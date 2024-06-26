﻿using RimWorld;

namespace ArtificialBeings
{
    [DefOf]
    public static class ABF_ThoughtDefOf
    {
        static ABF_ThoughtDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ABF_ThoughtDefOf));
        }

        [MayRequireSynstructsCore]
        public static ThoughtDef ABF_PersonalityShiftAllowed;

        [MayRequireSynstructsCore]
        public static ThoughtDef ABF_PersonalityShiftDenied;
    }
}
