﻿// SapientRulePackVariant.cs modified by Iron Wolf for Pawnmorph on 12/22/2019 8:17 AM
// last updated 12/22/2019  8:17 AM

using JetBrains.Annotations;
using Pawnmorph.FormerHumans;
using Verse;

namespace Pawnmorph.DefExtensions
{
    /// <summary>
    /// def extension for attaching variants of rule packs based on the sapient level 
    /// </summary>
    /// <seealso cref="Verse.DefModExtension" />
    public class SapientRulePackVariant : DefModExtension
    {
        /// <summary>
        /// The variants
        /// </summary>
        public SapientVariants<RulePackDef> variants;

        /// <summary>
        /// Gets the rule pack variant.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns></returns>
        [CanBeNull]
        public RulePackDef GetRulePackVariant(SapienceLevel level)
        {
            return variants?[level]; 
        }
    }
}