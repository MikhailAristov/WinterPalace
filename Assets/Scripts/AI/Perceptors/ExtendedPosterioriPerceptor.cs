using System.Collections;
using UnityEngine;

public class ExtendedPosterioriPerceptor : PosterioriPerceptor {

    protected KnowledgeState KNOW;

    // Return the index of the next player, according to turn history
    protected int NextPlayerIndex {
        get {
            int result = TurnHistory[NextTurnToAnalyze].Player.SittingOrder;
            do {
                result = (result + 1) % PlayerCount;
            } while(KNOW.IsPlayerKnockedOut(result));
            return result;
        }
    }

    public override bool SomeoneKnowsMyHand {
        get {
            for(int i = 0; i < PlayerCount; i++) {
                CardController mh = MyController.GetHand();
                if(i != MyController.SittingOrder && mh != null && KNOW.GetOpponentBeliefAboutMyHand(i) == mh.Value) {
                    return true;
                }
            }
            return false;
        }
    }

    // Set helpful pointers and initialize a knowledge object
    protected override void InitializeMemory() {
        Debug.Assert(!GameInitialized);
        Debug.Assert(MyController.Game != null);
        // Set turn history reference
        TurnHistory = MyController.Game.TurnHistory;
        // Create the knowledge database
        KNOW = new KnowledgeState(MyController.SittingOrder, MyController.Game.Players);
        PlayerCount = MyController.Game.Players.Length;
        // Initialize arrays inherited from superclass for consistency
        HandDistribution = new float[PlayerCount][];
        for(int i = 0; i < PlayerCount; i++) {
            HandDistribution[i] = new float[CARD_VECTOR_LENGTH];
        }
        DeckDistribution = new float[CARD_VECTOR_LENGTH];
        GameInitialized = true;
    }

    // Reset memory at the start of a round
    public override void ResetMemory() {
        if(!GameInitialized) {
            return;
        }
        Debug.Assert(!AnalysisOngoing);
        // Reset the next-to-analyze turn back to 0
        NextTurnToAnalyze = 0;
        // Reset knowledge
        KNOW.Reset();
        KNOW.MirrorCurrentBeliefs(ref HandDistribution, ref DeckDistribution);
        // The rest 
        HandInitialized = false;
        myHand = null;
        justDrawn = null;
        lastLearnedHandOf = null;
        lastLearnedCard = null;
        // After resetting memory, wait for the first card or cards to be drawn
        AnalysisOngoing = true;
        StartCoroutine(WaitForTheInitialDraw());
    }

    // Wait until the first card or two cards are drawn to account for them before proper analysis begins
    protected IEnumerator WaitForTheInitialDraw() {
        // First, wait until the intial draw has settled in
        yield return new WaitUntil(() => (myHand != null && justDrawn == null));
        KNOW.AccountForCard(myHand.Value);
        UpdateOwnHand();
        // If my turn is the very first one made, wait until the second card is drawn from deck to make a more intelligent move
        if(MyController.Game.CurrentPlayerIndex == MyController.SittingOrder) {
            // Now wait for the second draw
            yield return new WaitUntil(() => (myHand != null && justDrawn != null));
            // And update the distributions accordingly
            KNOW.AccountForCard(justDrawn.Value);
        }
        KNOW.RecalculateHandAndDeckdistributions();
        yield return new WaitForFixedUpdate();
        AnalysisOngoing = false;
    }

    // Analyze the most recent turn made in the game
    protected override IEnumerator AnalyzeTurn(int id) {
        Debug.Assert(TurnHistory != null && TurnHistory.Count >= id);
        Debug.Assert(id == NextTurnToAnalyze);
        // Get the actual turn
        MoveData turn = TurnHistory[id];

        // Precompute some helpful booleans
        bool ItWasMyTurn = (turn.Player == MyController);
        bool IWasTheTarget = (turn.Target == MyController);
        bool ThereWasAKnockOut = (turn.KnockedOut != null);
        bool PlayerWasKnockedOut = (ThereWasAKnockOut && turn.KnockedOut == turn.Player);
        bool TargetWasKnockedOut = (ThereWasAKnockOut && turn.KnockedOut == turn.Target);

        // Wait for some random time, just to ease the computational load per frame
        yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0, Time.fixedDeltaTime * MyController.Game.Players.Length));

        if(ItWasMyTurn) {
            // Update my own hand distribution, just for simplicity
            UpdateOwnHand();
            // Update my own play statistic
            UpdatePlayStatistics(turn);
            // Other players' knowledge of your hand becomes irrelevant after your own turn
            KNOW.ResetOpponentsBeliefsAboutMyHand();
            // We don't need to update unaccounted-for cards with the card just played and the card distribution, 
            // because that was done at the end of the last analysis run (of the player immediately before us)
        } else {
            KNOW.BasicOpponentTurnUpdate(turn);
        }

        // Wait some more
        yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0, Time.fixedDeltaTime * PlayerCount));

        // If there was a knock-out by something other than a Baron, then the filtering is simple
        if(ThereWasAKnockOut && turn.Card.Value != CardController.VALUE_BARON) {
            KNOW.KnockOutFilter(turn.KnockedOut.SittingOrder, turn.AdditionalDiscard.Value);
        } else if(!turn.NoEffect) {
            // Otherwise, if the card effect wasn't blocked by a Handmaid, we check specific card effects
            switch(turn.Card.Value) {
                case CardController.VALUE_GUARD:
                    // If the guard wasn't played against me and the target is still in the game,
                    // I at least know what their hand is NOT
                    if(!IWasTheTarget) {
                        KNOW.GuardFilter(turn.Target.SittingOrder, turn.TargetHandGuess);
                    }
                    break;
                case CardController.VALUE_PRIEST:
                    // If it was my turn, I now know the target's hand
                    if(ItWasMyTurn) {
                        // With a Priest, I now know the target's hand
                        Debug.Assert(turn.Target == lastLearnedHandOf);
                        KNOW.UpdateHandValueWithCertainty(lastLearnedHandOf.SittingOrder, lastLearnedCard.Value);
                    } else if(IWasTheTarget) {
                        // But if I was the target, the other player now knows my hand!
                        KNOW.SetOpponentBeliefAboutMyHand(turn.Player.SittingOrder, myHand.Value);
                    }
                    break;
                case CardController.VALUE_BARON:
                    // If either the player or the target were knocked out by Baron, we apply the Baron KO filter
                    if(PlayerWasKnockedOut) {
                        BaronEffectFilterWithKnockout(turn.Target.SittingOrder, turn.Player.SittingOrder, turn.AdditionalDiscard.Value);
                    } else if(TargetWasKnockedOut) {
                        BaronEffectFilterWithKnockout(turn.Player.SittingOrder, turn.Target.SittingOrder, turn.AdditionalDiscard.Value);
                    } else {
                        // On a draw between two other players, their hand distributions are now equal
                        BaronEffectFilterWithDraw(turn.Player.SittingOrder, turn.Target.SittingOrder);
                    }
                    break;
                case CardController.VALUE_PRINCE:
                    // If I was the target of the Prince, I just need to update my hand
                    if(IWasTheTarget) {
                        // Even if it was my turn, I have already accounted for my old hand and the card I just drew (both of which I have discarded),
                        // as well as have updated my own hand distribution, therefore I only need to account for the new card in my hand
                        Debug.Assert(DrawnBecauseOfThePrince != null);
                        KNOW.SetMyHand(DrawnBecauseOfThePrince.Value);
                        KNOW.AccountForCard(DrawnBecauseOfThePrince.Value);
                        DrawnBecauseOfThePrince = null;
                    } else {
                        // Otherwise, the special Prince filter logic applies (the values are automatically renormalized)
                        KNOW.PrinceFilterAndUpdate(turn.Target.SittingOrder, turn.AdditionalDiscard.Value);
                    }
                    break;
                case CardController.VALUE_KING:
                    // If I was involved in the swap, I know quite a lot about the other player's hand now (and vice versa)
                    if(ItWasMyTurn) {
                        KingEffectFilterInvolvingMe(turn.Target.SittingOrder);
                    } else if(IWasTheTarget) {
                        KingEffectFilterInvolvingMe(turn.Player.SittingOrder);
                    } else {
                        // If I wasn't involved, just swap the hand distributions of both players who were
                        KNOW.KingFilter(turn.Player.SittingOrder, turn.Target.SittingOrder);
                    }
                    break;
                default:
                    // Nothing to do when Handmaid, Countess, or Princess were played
                    break;
            }
        }

        // Wait some more
        yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0, Time.fixedDeltaTime * PlayerCount));

        // If I'm up next, wait until I've drawn my next card before finishing the analysis
        if(turn.Player != MyController && MyController.Game.Deck.CountCardsLeft > 1 && NextPlayerIndex == MyController.SittingOrder) {
            yield return new WaitUntil(() => ((myHand != null && justDrawn != null)) || MyController.Game.RoundOver);
            if(!MyController.Game.RoundOver) {
                // And update the distributions accordingly
                KNOW.AccountForCard(justDrawn.Value);
                KNOW.RecalculateHandAndDeckdistributions();
            }
        } else {
            KNOW.RecalculateHandAndDeckdistributions();
        }

        // Copy the arrays from the knowledge base to the ones inherited from the superclass, just for consistency
        KNOW.MirrorCurrentBeliefs(ref HandDistribution, ref DeckDistribution);

        // Finally, finish the update and return
        NextTurnToAnalyze = id + 1;
        yield return new WaitForFixedUpdate();
        AnalysisOngoing = false;
    }

    // Update own hand in the knowledge object
    protected void UpdateOwnHand() {
        if(myHand != null) {
            KNOW.SetMyHand(myHand.Value);
        } else if(justDrawn != null) {
            KNOW.SetMyHand(justDrawn.Value);
        } else {
            Debug.LogErrorFormat("{0}'s perceptor wants to update his own hand distribution, but his hand is empty!", MyController);
        }
    }

    // If the Baron has knocked a player out, it gives us a lot of information about the winner's hand
    protected override void BaronEffectFilterWithKnockout(int winnerIndex, int loserIndex, int LosersHandValue) {
        if(winnerIndex != MyController.SittingOrder) {
            KNOW.BaronFilter(winnerIndex, loserIndex, LosersHandValue);
        } else {
            // The knocked-out player has no cards anymore
            KNOW.KnockOutFilter(loserIndex, LosersHandValue);
        }
    }

    // If a Baron play ended in a draw, it's also a LOT of information
    protected override void BaronEffectFilterWithDraw(int playerIndex, int targetIndex) {
        // If I was involved in the draw in any way, I now know that the other player has the same card as me
        if(playerIndex == MyController.SittingOrder) {
            KNOW.UpdateHandValueWithCertainty(targetIndex, myHand.Value);
            KNOW.SetOpponentBeliefAboutMyHand(targetIndex, myHand.Value);
        } else if(targetIndex == MyController.SittingOrder) {
            KNOW.UpdateHandValueWithCertainty(playerIndex, myHand.Value);
            KNOW.SetOpponentBeliefAboutMyHand(playerIndex, myHand.Value);
        } else {
            KNOW.BaronFilter(playerIndex, targetIndex);
        }
    }

    // If a King was played by or against me, I need to update my hand, but I also know the other player's hand now (and vice versa)
    protected void KingEffectFilterInvolvingMe(int OtherPlayerIndex) {
        Debug.Assert(MyHandBeforeKing != null);
        KNOW.KingFilter(OtherPlayerIndex, MyHandBeforeKing.Value, myHand.Value);
        MyHandBeforeKing = null;
    }

    /*
	 * The rest are simple getter methods from the base class 
	 */
    public override float GetCardProbabilityInDeck(int CardValue) {
        Debug.Assert(!AnalysisOngoing);
        Debug.Assert(KNOW != null);
        Debug.Assert(CardValue >= 0 && CardValue <= CardController.VALUE_PRINCESS);
        return KNOW.GetDeckProbability(CardValue);
    }

    public override float GetCardProbabilityInHand(PlayerController Player, int CardValue) {
        Debug.Assert(!AnalysisOngoing);
        Debug.Assert(KNOW != null);
        Debug.Assert(MyController.Game != null);
        Debug.Assert(Player != null);
        Debug.Assert(CardValue >= 0 && CardValue <= CardController.VALUE_PRINCESS);
        return KNOW.GetHandProbability(Player.SittingOrder, CardValue);
    }

    public override void RevealHand(PlayerController toPlayer) {
        Debug.Assert(!AnalysisOngoing);
        KNOW.SetOpponentBeliefAboutMyHand(toPlayer.SittingOrder, MyController.GetHand().Value);
    }

    public override void LearnHand(PlayerController ofPlayer, CardController card) {
        Debug.Assert(!AnalysisOngoing);
        lastLearnedHandOf = ofPlayer;
        lastLearnedCard = card;
    }

    public override int PlayerThinksMyHandIs(PlayerController p) {
        Debug.Assert(p != null);
        Debug.Assert(p != MyController);
        return KNOW.GetOpponentBeliefAboutMyHand(p.SittingOrder);
    }

    public override int HowOftenHasPlayerTargetedMe(PlayerController p) {
        Debug.Assert(p != null);
        Debug.Assert(p != MyController);
        return KNOW.CountTargetingsByOpponent(p.SittingOrder);
    }

    public override int HowManyOthersHasPlayerKnockedOut(PlayerController p) {
        Debug.Assert(p != null);
        Debug.Assert(p != MyController);
        return KNOW.CountKnockoutsByOpponent(p.SittingOrder);
    }
}
