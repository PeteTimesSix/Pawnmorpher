﻿// MorphDef.cs modified by Iron Wolf for Pawnmorph on 08/02/2019 2:32 PM
// last updated 08/02/2019  2:32 PM

using System.Collections.Generic;
using Verse;

namespace Pawnmorph
{
    /// <summary>
    /// def class for a 'morph' 
    /// </summary>
    public class MorphDef : Def
    {
        /// <summary>
        /// all categories the morph belongs to (canid, carnivore, ect) 
        /// </summary>
        public List<string> categories = new List<string>(); 
        public ThingDef race; //the animal race of the morph 

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string configError in base.ConfigErrors())
            {
                yield return configError;
            }

            if (race == null)
            {
                yield return $"no race def found!"; 
            }else if (race.race == null)
            {
                yield return $"race {race.defName} has no race properties! are you sure this is a race?"; 
            }
        }
    }
}