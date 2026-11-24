using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using HexerMod.Models;

namespace HexerMod.Core
{
    public static class HexerCombatHookSubscriber
    {
        private static readonly ConditionalWeakTable<CombatState, Hexer> _subscribers = new();

        public static IEnumerable<AbstractModel> GetSubscribers(CombatState combatState)
        {
            if (combatState == null || !ModelDb.Contains(typeof(Hexer)))
            {
                yield break;
            }

            if (!_subscribers.TryGetValue(combatState, out var subscriber))
            {
                subscriber = (Hexer)ModelDb.Character<Hexer>().MutableClone();
                _subscribers.Add(combatState, subscriber);
            }

            yield return subscriber;
        }
    }
}
