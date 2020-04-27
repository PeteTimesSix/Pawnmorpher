﻿using Pawnmorph.Dialogs;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Pawnmorph
{
    /// <summary>
    /// Thing comp that applies a ColorationAspect with dialog-chosen colors
    /// </summary>
    public class Comp_PlayerPickedRecoloration : CompUseEffect
    {
        /// <summary>
        /// Apply effect on use
        /// </summary>
        /// <param name="usedBy">Pawn using parent thing</param>
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            ColonistColorPicker.showDialogForPawn(usedBy);
        }
    }
}
