using System;
using UnityEngine;
using Distribution1D = MatrixToolkit.ProbabilityDistribution1D;
using Distribution2D = MatrixToolkit.ProbabilityDistribution2D;
using Distribution3D = MatrixToolkit.ProbabilityDistribution3D;

public class KnowledgeState {

    public const int CARD_VECTOR_LENGTH = CardController.VALUE_PRINCESS;

    protected int PlayerCount;
    protected int CurrentOpponentCount;
    protected int[] HiddenHands;

    protected int MySittingOrder;
    protected int MyHandValue;
    protected int JustDrawnValue;

    protected int[] CountUnaccountedForCards;
    protected Distribution1D DeckDistribution;
    protected DeckController TheDeck;

    // Several data structures for easier reference
    protected Distribution1D SingleOpponentHandDistribution;
    protected Distribution2D TwoOpponentsHandsDistribution;
    protected Distribution3D ThreeOpponentsHandsDistribution;

    protected Distribution1D[] HandDistribution;

    protected bool[] PlayerIsKnockedOut;
    protected int[] PlayerKnowsThatMyHandIs;
    protected int[] PlayerHasTargetedMe;
    protected int[] PlayerHasKnockouts;

    // These values are precomputed once for easier access
    protected static bool PrecomputationsComplete;
    protected static Distribution1D BaseDeckDistribution;
    protected static Distribution3D BaseThreeOpponentsHandsDistribution;

    // Performance optimization
    protected Distribution1D tempArray1D = new Distribution1D(CARD_VECTOR_LENGTH);
    protected Distribution2D tempArray2D = new Distribution2D(CARD_VECTOR_LENGTH, CARD_VECTOR_LENGTH);
    protected Distribution3D tempArray3D = new Distribution3D(CARD_VECTOR_LENGTH, CARD_VECTOR_LENGTH, CARD_VECTOR_LENGTH);
    protected Distribution1D virtualDeckDistribution = new Distribution1D(CARD_VECTOR_LENGTH);
    protected int[] virtualRemainingCards = new int[CARD_VECTOR_LENGTH];

    public KnowledgeState(int ThisPlayerSittingOrder, PlayerController[] AllPlayers) {
        // Save the player data
        PlayerCount = AllPlayers.Length;
        MySittingOrder = ThisPlayerSittingOrder;
        HiddenHands = new int[PlayerCount - 1];
        // Initialize distributions
        DeckDistribution = new Distribution1D(CARD_VECTOR_LENGTH);
        SingleOpponentHandDistribution = new Distribution1D(CARD_VECTOR_LENGTH);
        TwoOpponentsHandsDistribution = new Distribution2D(CARD_VECTOR_LENGTH, CARD_VECTOR_LENGTH);
        ThreeOpponentsHandsDistribution = new Distribution3D(CARD_VECTOR_LENGTH, CARD_VECTOR_LENGTH, CARD_VECTOR_LENGTH);
        HandDistribution = new Distribution1D[PlayerCount];
        for(int p = 0; p < PlayerCount; p++) {
            HandDistribution[p] = new Distribution1D(CARD_VECTOR_LENGTH);
        }
        CountUnaccountedForCards = new int[GameController.CARD_COUNT.Length];
        TheDeck = AllPlayers[ThisPlayerSittingOrder].Game.Deck;
        // Perform precomputations of static variables if necessary
        if(!PrecomputationsComplete) {
            PrecomputationsComplete = true;
            PrecomputeStaticArrays();
        }
        // Other opponent data
        PlayerIsKnockedOut = new bool[PlayerCount];
        PlayerKnowsThatMyHandIs = new int[PlayerCount];
        PlayerHasTargetedMe = new int[PlayerCount];
        PlayerHasKnockouts = new int[PlayerCount];
        // Initialize memory
        Reset();
    }

    // Resets the memory of the knowledge state at the start of each round
    public void Reset() {
        CurrentOpponentCount = PlayerCount - 1;
        SetMyHand(0, 0);
        Array.Clear(PlayerIsKnockedOut, 0, PlayerCount);
        Array.Copy(GameController.CARD_COUNT, CountUnaccountedForCards, GameController.CARD_COUNT.Length);
        DeckDistribution.CopyFrom(BaseDeckDistribution);
        ThreeOpponentsHandsDistribution.CopyFrom(BaseThreeOpponentsHandsDistribution);
        ResetOpponentsBeliefsAboutMyHand();
        // Sort the hidden hands' sitting order again
        int hiddenHandIndex = 0;
        for(int p = 0; p < PlayerCount; p++) {
            if(p != MySittingOrder) {
                HiddenHands[hiddenHandIndex++] = p;
                HandDistribution[p].CopyFrom(BaseDeckDistribution);
            }
        }
    }

    // Attempts to strike the specified card from the unaccounted-for list
    // Throws exception if all cards of this type already already accounted for
    public void AccountForCard(int Value) {
        Debug.Assert(Value >= CardController.VALUE_GUARD && Value <= CardController.VALUE_PRINCESS);
        if(CountUnaccountedForCards[Value] > 0) {
            CountUnaccountedForCards[Value] -= 1;
        } else {
            throw new ArgumentOutOfRangeException(string.Format("Player {0} cannot account for {1}, because all cards of this value are already accounted for!", MySittingOrder, CardController.NAMES[Value]));
        }
    }

    // Updates own hand
    public void SetMyHand(int Hand, int JustDrawn = 0) {
        Debug.Assert(Hand >= 0 && Hand <= CardController.VALUE_PRINCESS);
        Debug.Assert(JustDrawn >= 0 && JustDrawn <= CardController.VALUE_PRINCESS);
        MyHandValue = Hand;
        JustDrawnValue = JustDrawn;
        // Also update own hand distribution
        HandDistribution[MySittingOrder].Clear();
        if(Hand > 0) {
            HandDistribution[MySittingOrder][Hand - 1] = 1f;
        }
    }

    // Precomputes the static arrays for quicker access
    protected static void PrecomputeStaticArrays() {
        // Initialized base deck distribution
        BaseDeckDistribution = new Distribution1D(CARD_VECTOR_LENGTH);
        for(int CardIndex = 0; CardIndex < CARD_VECTOR_LENGTH; CardIndex++) {
            BaseDeckDistribution[CardIndex] = ((float)GameController.CARD_COUNT[CardIndex + 1]) / GameController.TOTAL_CARD_COUNT;
        }
        // Initialize base joint hand distribution
        BaseThreeOpponentsHandsDistribution = new Distribution3D(CARD_VECTOR_LENGTH, CARD_VECTOR_LENGTH, CARD_VECTOR_LENGTH);
        // Optimization for the following calculation:
        int[] tempRemainingCards = new int[CARD_VECTOR_LENGTH]; // We can't use virtualRemainingCards in a static method...
        Array.Copy(GameController.CARD_COUNT, 1, tempRemainingCards, 0, CARD_VECTOR_LENGTH);
        float SumOfArray = 0, ScalingFactor = 1f / GameController.TOTAL_CARD_COUNT / (GameController.TOTAL_CARD_COUNT - 1) / (GameController.TOTAL_CARD_COUNT - 2);
        // Calculate base joint hand distribution
        for(int Hand1Index = 0; Hand1Index < CARD_VECTOR_LENGTH; Hand1Index++) {
            // Account for this value of the first hand
            float Hand1Prob = ScalingFactor * tempRemainingCards[Hand1Index];
            tempRemainingCards[Hand1Index] -= 1;
            // And loop through all possible second and third hidden hands
            for(int Hand2Index = 0; Hand2Index < CARD_VECTOR_LENGTH; Hand2Index++) {
                // Account for this value of the second hand
                float Hand2JointProb = Hand1Prob * tempRemainingCards[Hand2Index];
                tempRemainingCards[Hand2Index] -= 1;
                // And loop through all possible third hidden hands
                for(int Hand3Index = 0; Hand3Index < CARD_VECTOR_LENGTH; Hand3Index++) {
                    // If this is an impossible case, just jump to the next iteration
                    if(Hand2JointProb <= 0 || tempRemainingCards[Hand3Index] <= 0) {
                        continue;
                    }
                    // Otherwise, calculate the joint probability of this case and store it
                    float jointProb = Hand2JointProb * tempRemainingCards[Hand3Index];
                    BaseThreeOpponentsHandsDistribution[Hand1Index, Hand2Index, Hand3Index] = jointProb;
                    SumOfArray += jointProb;
                }
                // Reset card counts for the next iteration
                tempRemainingCards[Hand2Index] += 1;
            }
            // Reset card counts for the next iteration
            tempRemainingCards[Hand1Index] += 1;
        }
        Debug.Assert(AIUtil.Approx(SumOfArray, 1f));
    }

    // After each opponent's turn, their hand distribution and statistics can be updated
    public void BasicOpponentTurnUpdate(MoveData Turn) {
        Debug.Assert(Turn != null);
        Debug.Assert(Turn.Player.SittingOrder != MySittingOrder);
        // When analyzing another player's turn, filter their hand first:
        FilterHiddenHandWithPlayedCard(Turn.Player.SittingOrder, Turn.Card.Value);
        // The card they just played lands on the discard pile, so update the unaccounted-for list
        AccountForCard(Turn.Card.Value);
        // Also update this player's stats
        if(Turn.Target != null && Turn.Target.SittingOrder == MySittingOrder) {
            BumpTargetingsByOpponent(Turn.Player.SittingOrder);
        }
        if(Turn.KnockedOut != null && Turn.KnockedOut != Turn.Player) {
            BumpKnockoutsByOpponent(Turn.Player.SittingOrder);
        }
    }

    // During the basic opponent turn update, we need to update the joint probability distribution
    // using just the information about which card he has just played
    protected void FilterHiddenHandWithPlayedCard(int OpponentSittingOrder, int CardValue) {
        Debug.Assert(OpponentSittingOrder != MySittingOrder);
        Debug.Assert(!PlayerIsKnockedOut[OpponentSittingOrder]);
        Debug.Assert(CardValue >= CardController.VALUE_GUARD && CardValue <= CardController.VALUE_PRINCESS);
        // Find the card's and the player's hidden hand index
        int CardIndex = CardValue - 1;
        int PlayerIndex = GetHiddenHandIndex(OpponentSittingOrder);
        // Calculate the transition probabilities
        float ProbabilityOfPlayingFromHAND = GetProbabilityOfPlayingFromHand(OpponentSittingOrder, CardValue);
        float ProbabilityOfPlayingFromDECK = 1f - ProbabilityOfPlayingFromHAND;
        // Update the hand distribution matrices, but only if there is any chance that the player played 
        // from own hand -- otherwise, their hand distribution doesn't change at this point, anyway
        if(ProbabilityOfPlayingFromHAND > 0) {
            if(CurrentOpponentCount == 3) {
                // Get a slice of the 3D matrix corrensponding to the player having had the played card
                tempArray2D = ThreeOpponentsHandsDistribution.GetSlice(PlayerIndex, CardIndex);
                // Tensor the slice with the current deck distribution
                tempArray3D = CalculateHandsFromSliceOnDraw(tempArray2D, PlayerIndex);
                // Merge the tensor back into the 3D matrix, using the play probabilities earlier as weights
                ThreeOpponentsHandsDistribution.Add(tempArray3D, ProbabilityOfPlayingFromDECK, ProbabilityOfPlayingFromHAND);
                ThreeOpponentsHandsDistribution.Renormalize();
            } else if(CurrentOpponentCount == 2) {
                tempArray1D = TwoOpponentsHandsDistribution.GetSlice(PlayerIndex, CardIndex);
                tempArray2D = CalculateHandsFromSliceOnDraw(tempArray1D, PlayerIndex);
                TwoOpponentsHandsDistribution.Add(tempArray2D, ProbabilityOfPlayingFromDECK, ProbabilityOfPlayingFromHAND);
                TwoOpponentsHandsDistribution.Renormalize();
            } else if(CurrentOpponentCount == 1) {
                tempArray1D = CalculateHandsFromSliceOnDraw();
                SingleOpponentHandDistribution.Add(tempArray1D, ProbabilityOfPlayingFromDECK, ProbabilityOfPlayingFromHAND);
                SingleOpponentHandDistribution.Renormalize();
            }
        }
        // Finally, recalculate utility arrays
        RecalculateHandAndDeckdistributions();
    }

    // Using the empiric values from PosterioriPerceptor.LikelihoodOfPlay, as well as the current hand and deck distribution,
    // return the probability (not likelihood!) that the player played the hand he played from hand, rather than from deck
    protected float GetProbabilityOfPlayingFromHand(int OpponentSittingOrder, int CardValue) {
        Debug.Assert(OpponentSittingOrder != MySittingOrder);
        Debug.Assert(CardValue >= CardController.VALUE_GUARD && CardValue <= CardController.VALUE_PRINCESS);
        int PlayedCardIndex = CardValue - 1;
        // Calculate the likelihoods that the player immediately played the card they just drew,
        // or that they played the current hand from their hand
        float probPlayFromDECK = 0, probPlayFromHAND = 0;
        for(int otherCardIndex = 0; otherCardIndex < CARD_VECTOR_LENGTH; otherCardIndex++) {
            probPlayFromDECK += PosterioriPerceptor.LikelihoodOfPlay[CardValue, otherCardIndex + 1] * HandDistribution[OpponentSittingOrder][otherCardIndex];
            probPlayFromHAND += PosterioriPerceptor.LikelihoodOfPlay[CardValue, otherCardIndex + 1] * DeckDistribution[otherCardIndex];
        }
        // Multiply each likelihood by the probability of each the played card being, respectively, in the deck, or in the hand
        // It's OK if they don't sum up to 1: we will renormalize everything together at the end
        probPlayFromDECK *= DeckDistribution[PlayedCardIndex];
        probPlayFromHAND *= HandDistribution[OpponentSittingOrder][PlayedCardIndex];
        // Normalize and return the probability
        return probPlayFromHAND / (probPlayFromHAND + probPlayFromDECK);
    }

    // On a knock-out, reduce the dimension of the joint distribution array and renormalize it
    public void KnockOutFilter(int SittingOrder, int DiscardedValue) {
        Debug.Assert(SittingOrder != MySittingOrder);
        Debug.Assert(DiscardedValue >= CardController.VALUE_GUARD && DiscardedValue <= CardController.VALUE_PRINCESS);
        int CardIndex = DiscardedValue - 1;
        int HiddenHandIndex = GetHiddenHandIndex(SittingOrder);
        // Account for the discared card
        AccountForCard(DiscardedValue);
        // Merge down the current matrix to one dimension lower, taking the slice that corresponds to the knocked-out player's actual hand
        if(CurrentOpponentCount == 3) {
            TwoOpponentsHandsDistribution = ThreeOpponentsHandsDistribution.GetSlice(HiddenHandIndex, CardIndex);
            TwoOpponentsHandsDistribution.Renormalize();
            // Also, we move out the knocked-out player's sitting order to the last place of the HiddenHands array
            AIUtil.ShiftToLast(ref HiddenHands, HiddenHandIndex);
        } else if(CurrentOpponentCount == 2) {
            SingleOpponentHandDistribution = TwoOpponentsHandsDistribution.GetSlice(HiddenHandIndex, CardIndex);
            SingleOpponentHandDistribution.Renormalize();
            AIUtil.ShiftToLast(ref HiddenHands, HiddenHandIndex);
        }
        // Update player situation
        CurrentOpponentCount -= 1;
        PlayerIsKnockedOut[SittingOrder] = true;
    }

    // Card filter: GUARD, when played against anyone but me.
    public void GuardFilter(int TargetSittingOrder, int GuessedValue) {
        Debug.Assert(TargetSittingOrder != MySittingOrder);
        Debug.Assert(GuessedValue >= CardController.VALUE_PRIEST && GuessedValue <= CardController.VALUE_PRINCESS);
        int CardIndex = GuessedValue - 1;
        int HiddenHandIndex = GetHiddenHandIndex(TargetSittingOrder);
        // Update and renormalize the appropriate matrix
        if(CurrentOpponentCount == 3) {
            // In case of THREE remaining opponents, update the ThreeOpponentsHandsDistribution
            ThreeOpponentsHandsDistribution.ClearSlice(HiddenHandIndex, CardIndex);
            ThreeOpponentsHandsDistribution.Renormalize();
        } else if(CurrentOpponentCount == 2) {
            // In case of TWO remaining opponents, update the TwoOpponentsHandsDistribution
            TwoOpponentsHandsDistribution.ClearSlice(HiddenHandIndex, CardIndex);
            TwoOpponentsHandsDistribution.Renormalize();
        } else if(CurrentOpponentCount == 1) {
            // In case of ONE remaining opponents, update the SingleOpponentHandDistribution
            SingleOpponentHandDistribution[CardIndex] = 0;
            SingleOpponentHandDistribution.Renormalize();
        }
    }

    // Card filter: BARON, when it resulted in a knock-out and I was NOT involved.
    public void BaronFilter(int WinnerSittingOrder, int LoserSittingOrder, int LosersHandValue) {
        Debug.Assert(WinnerSittingOrder != MySittingOrder);
        Debug.Assert(LoserSittingOrder != MySittingOrder);
        Debug.Assert(LosersHandValue >= CardController.VALUE_GUARD && LosersHandValue <= CardController.VALUE_PRINCESS);
        // First of all, we can shrink down the matrix by knockout
        KnockOutFilter(LoserSittingOrder, LosersHandValue);
        // Secondly, we know that the winner has a hand higher than LosersHandValue,
        // so we can set all other matrix elements to zero
        int WinnerIndex = GetHiddenHandIndex(WinnerSittingOrder);
        if(CurrentOpponentCount == 2) {
            // LosersHandValue is used here to avoid awkward "- 1 + 1" clauses
            TwoOpponentsHandsDistribution.ClearSliceBelowIndex(WinnerIndex, LosersHandValue);
            TwoOpponentsHandsDistribution.Renormalize();
        } else if(CurrentOpponentCount == 1) {
            for(int i = 0; i < LosersHandValue; i++) {
                SingleOpponentHandDistribution[i] = 0;
            }
            SingleOpponentHandDistribution.Renormalize();
        }
    }

    // Card filter: BARON, when it did NOT result in a knock-out and I was NOT involved.
    public void BaronFilter(int PlayerSittingOrder, int TargetSittingOrder) {
        Debug.Assert(PlayerSittingOrder != MySittingOrder);
        Debug.Assert(TargetSittingOrder != MySittingOrder);
        if(CurrentOpponentCount == 3) {
            int PlayerIndex = GetHiddenHandIndex(PlayerSittingOrder);
            int TargetIndex = GetHiddenHandIndex(TargetSittingOrder);
            // This call sets  all elements on the diagonal spanned by the PlayerIndex and TargetIndex axes
            ThreeOpponentsHandsDistribution.ClearAllButDiagonal(3 - PlayerIndex - TargetIndex);
            ThreeOpponentsHandsDistribution.Renormalize();
        } else if(CurrentOpponentCount == 2) {
            TwoOpponentsHandsDistribution.ClearAllButDiagonal();
            TwoOpponentsHandsDistribution.Renormalize();
        }
    }

    // Card filter: PRINCE, played against one of the opponents.
    public void PrinceFilterAndUpdate(int TargetSittingOrder, int DiscardedHandValue) {
        Debug.Assert(TargetSittingOrder != MySittingOrder);
        Debug.Assert(DiscardedHandValue >= CardController.VALUE_GUARD && DiscardedHandValue <= CardController.VALUE_PRINCESS);
        int CardIndex = DiscardedHandValue - 1;
        int HiddenHandIndex = GetHiddenHandIndex(TargetSittingOrder);
        // Account for the discarded card
        AccountForCard(DiscardedHandValue);
        // Then, update the appropriate matrices
        if(CurrentOpponentCount == 3) {
            // Get a slice of the 3D matrix corrensponding to the player having had the played card
            tempArray2D = ThreeOpponentsHandsDistribution.GetSlice(HiddenHandIndex, CardIndex);
            // Tensor the slice with the current deck distribution back into ThreeOpponentsHandsDistribution
            ThreeOpponentsHandsDistribution = CalculateHandsFromSliceOnDraw(tempArray2D, HiddenHandIndex);
        } else if(CurrentOpponentCount == 2) {
            tempArray1D = TwoOpponentsHandsDistribution.GetSlice(HiddenHandIndex, CardIndex);
            TwoOpponentsHandsDistribution = CalculateHandsFromSliceOnDraw(tempArray1D, HiddenHandIndex);
        } else if(CurrentOpponentCount == 1) {
            SingleOpponentHandDistribution = CalculateHandsFromSliceOnDraw();
        }
    }

    // Card filter: KING, when I am involved.
    public void KingFilter(int OtherPlayerSittingOrder, int MyHandValueBefore, int MyHandValueAfter) {
        Debug.Assert(OtherPlayerSittingOrder != MySittingOrder);
        Debug.Assert(MyHandValueBefore >= CardController.VALUE_GUARD && MyHandValueBefore <= CardController.VALUE_PRINCESS);
        Debug.Assert(MyHandValueAfter >= CardController.VALUE_GUARD && MyHandValueAfter <= CardController.VALUE_PRINCESS);
        int CardIndexBefore = MyHandValueBefore - 1, CardIndexAfter = MyHandValueAfter - 1;
        int OpponentIndex = GetHiddenHandIndex(OtherPlayerSittingOrder);
        // First, we update the joint distribution with the knowledge of the opponent's hand as it was BEFORE the swap
        // Note that BEFORE, the opponent had the card that I have AFTER.
        UpdateHandValueWithCertainty(OtherPlayerSittingOrder, MyHandValueAfter);
        // Secondly, however, we swap the slices corresponding to the opponent's hand BEFORE and AFTER,
        // to account for our knowledge, that the opponent now has our old hand, i.e. what I had BEFORE.
        if(CurrentOpponentCount == 3) {
            ThreeOpponentsHandsDistribution.SwapSlices(OpponentIndex, CardIndexBefore, CardIndexAfter);
        } else if(CurrentOpponentCount == 2) {
            TwoOpponentsHandsDistribution.SwapSlices(OpponentIndex, CardIndexBefore, CardIndexAfter);
        } else if(CurrentOpponentCount == 1) {
            SingleOpponentHandDistribution.Swap(CardIndexBefore, CardIndexAfter);
        }
        // The opponent also knows my hand
        SetOpponentBeliefAboutMyHand(OtherPlayerSittingOrder, MyHandValueBefore);
        // I also need stop accounting for my old hand, and start accounting for my new one
        CountUnaccountedForCards[MyHandValueBefore] += 1;
        AccountForCard(MyHandValueAfter);
    }

    // Card filter: KING, when I am NOT involved.
    public void KingFilter(int PlayerSittingOrder, int TargetSittingOrder) {
        Debug.Assert(PlayerSittingOrder != MySittingOrder);
        Debug.Assert(TargetSittingOrder != MySittingOrder);
        // It is as simple as transposing the corresponding matrix
        if(CurrentOpponentCount == 3) {
            // For the 3D matrix, we need to transpose it around one of the axes, namely that not involved in the swap
            int uninvolvedHiddenHand = -1;
            for(int p = 0; p < PlayerCount; p++) {
                if(p != MySittingOrder && p != PlayerSittingOrder && p != TargetSittingOrder) {
                    uninvolvedHiddenHand = GetHiddenHandIndex(p);
                    break;
                }
            }
            ThreeOpponentsHandsDistribution.Transpose(uninvolvedHiddenHand);
        } else if(CurrentOpponentCount == 2) {
            TwoOpponentsHandsDistribution.Transpose();
        }
    }

    // If we know for certain the value of an opponents hand, we can set all other slices of the joint distribution to zero
    public void UpdateHandValueWithCertainty(int SittingOrder, int CardValue) {
        Debug.Assert(SittingOrder != MySittingOrder);
        Debug.Assert(CardValue >= CardController.VALUE_GUARD && CardValue <= CardController.VALUE_PRINCESS);
        int CardIndex = CardValue - 1;
        int HiddenHandIndex = GetHiddenHandIndex(SittingOrder);
        // Update and renormalize the appropriate matrix
        if(CurrentOpponentCount == 3) {
            ThreeOpponentsHandsDistribution.ClearAllButSlice(HiddenHandIndex, CardIndex);
            ThreeOpponentsHandsDistribution.Renormalize();
        } else if(CurrentOpponentCount == 2) {
            TwoOpponentsHandsDistribution.ClearAllButSlice(HiddenHandIndex, CardIndex);
            TwoOpponentsHandsDistribution.Renormalize();
        } else if(CurrentOpponentCount == 1) {
            SingleOpponentHandDistribution.Clear();
            SingleOpponentHandDistribution[CardIndex] = 1f;
        }
    }

    // In case of ONE opponent remaining,
    // calculate his hand distribution after drawing a new card.
    protected Distribution1D CalculateHandsFromSliceOnDraw() {
        // Count up all the unaccounted-for cards and calculate a distribution over them
        Distribution1D result = new Distribution1D(CARD_VECTOR_LENGTH);
        for(int i = 0; i < CARD_VECTOR_LENGTH; i++) {
            result[i] = CountUnaccountedForCards[i + 1];
        }
        result.Renormalize();
        return result;
    }

    // In case of TWO opponents remaining,
    // calculate the joint hands distribution after player with DrawerIndex drew a card from deck,
    // while the other player's hand distribution is stored in OtherPlayersHand array.
    protected Distribution2D CalculateHandsFromSliceOnDraw(Distribution1D OtherPlayersHand, int DrawerIndex) {
        Array.Copy(CountUnaccountedForCards, 1, virtualRemainingCards, 0, CARD_VECTOR_LENGTH);
        //AIUtil.DisplayVector("virtualRemainingCards " + MySittingOrder, virtualRemainingCards);
        //AIUtil.DisplayVector("OtherPlayersHand" + MySittingOrder, OtherPlayersHand);
        // Prepare virtual deck distribution
        virtualDeckDistribution.Clear();
        for(int h1 = 0; h1 < CARD_VECTOR_LENGTH; h1++) {
            virtualRemainingCards[h1] -= 1;
            CumulativeUpdateDeckDistribution(OtherPlayersHand[h1], virtualRemainingCards, ref virtualDeckDistribution);
            virtualRemainingCards[h1] += 1;
            //AIUtil.DisplayVector("virtualDeckDistribution " + MySittingOrder, virtualDeckDistribution);
        }
        virtualDeckDistribution.Renormalize();
        // Build the output from tensor of the slice and the virtual deck distribution
        Distribution2D result = OtherPlayersHand.GetTensorProduct(virtualDeckDistribution);
        if(DrawerIndex == 0) {
            result.Transpose();
        }
        // Lastly, renormalize the 2D distribution
        result.Renormalize();
        return result;
    }

    // In case of THREE opponents remaining,
    // calculate the joint hands distribution after player with DrawerIndex drew a card from deck,
    // while the other two players' hand distributions are stored in OtherPlayersHands array.
    protected Distribution3D CalculateHandsFromSliceOnDraw(Distribution2D OtherPlayersHands, int DrawerIndex) {
        Array.Copy(CountUnaccountedForCards, 1, virtualRemainingCards, 0, CARD_VECTOR_LENGTH);
        // Prepare virtual deck distribution
        virtualDeckDistribution.Clear();
        for(int h1 = 0; h1 < CARD_VECTOR_LENGTH; h1++) {
            virtualRemainingCards[h1] -= 1;
            for(int h2 = 0; h2 < CARD_VECTOR_LENGTH; h2++) {
                virtualRemainingCards[h2] -= 1;
                CumulativeUpdateDeckDistribution(OtherPlayersHands[h1, h2], virtualRemainingCards, ref virtualDeckDistribution);
                virtualRemainingCards[h2] += 1;
            }
            virtualRemainingCards[h1] += 1;
        }
        virtualDeckDistribution.Renormalize();
        // Build the output from tensor of the slice and the virtual deck distribution
        Distribution3D result = OtherPlayersHands.GetTensorProduct(virtualDeckDistribution);
        // Rotate the tensor back into the proper alignment with the hidden hand indices
        if(DrawerIndex < 2) {
            result.Transpose(0);
            if(DrawerIndex < 1) {
                result.Transpose(2);
            }
        }
        // Lastly, renormalize the 2D distribution
        result.Renormalize();
        return result;
    }

    // Calculate deck distribution with just ONE opponent remaining
    protected void CalculateDeckDistribution(Distribution1D HiddenHandDistribution, ref int[] RemainingCards, ref Distribution1D outDeckDistribution) {
        for(int h1 = 0; h1 < CARD_VECTOR_LENGTH; h1++) {
            RemainingCards[h1] -= 1;
            CumulativeUpdateDeckDistribution(HiddenHandDistribution[h1], RemainingCards, ref outDeckDistribution);
            RemainingCards[h1] += 1;
        }
    }

    // Calculate deck distribution with just TWO opponents remaining
    protected void CalculateDeckDistribution(Distribution2D HiddenHandDistribution, ref int[] RemainingCards, ref Distribution1D outDeckDistribution) {
        for(int h1 = 0; h1 < CARD_VECTOR_LENGTH; h1++) {
            RemainingCards[h1] -= 1;
            for(int h2 = 0; h2 < CARD_VECTOR_LENGTH; h2++) {
                RemainingCards[h2] -= 1;
                CumulativeUpdateDeckDistribution(HiddenHandDistribution[h1, h2], RemainingCards, ref outDeckDistribution);
                RemainingCards[h2] += 1;
            }
            RemainingCards[h1] += 1;
        }
    }

    // Calculate deck distribution with all THREE players remaining
    protected void CalculateDeckDistribution(Distribution3D HiddenHandDistribution, ref int[] RemainingCards, ref Distribution1D outDeckDistribution) {
        for(int h1 = 0; h1 < CARD_VECTOR_LENGTH; h1++) {
            RemainingCards[h1] -= 1;
            for(int h2 = 0; h2 < CARD_VECTOR_LENGTH; h2++) {
                RemainingCards[h2] -= 1;
                for(int h3 = 0; h3 < CARD_VECTOR_LENGTH; h3++) {
                    RemainingCards[h3] -= 1;
                    CumulativeUpdateDeckDistribution(HiddenHandDistribution[h1, h2, h3], RemainingCards, ref outDeckDistribution);
                    RemainingCards[h3] += 1;
                }
                RemainingCards[h2] += 1;
            }
            RemainingCards[h1] += 1;
        }
    }

    // The joint probabilities array is good for analysis, but slow for quick access,
    // so we instead popular smaller, 1D arrays for each hand, as well as for the deck.
    public void RecalculateHandAndDeckdistributions() {
        // The computation only works if there ARE any opponents left
        if(CurrentOpponentCount < 1) {
            return;
        }
        // Precomputation for the aggregated hand distributions update
        for(int h = 0; h < HiddenHands.Length; h++) {
            HandDistribution[HiddenHands[h]].Clear();
        }
        DeckDistribution.Clear();
        // Precomputations for the deck distribution update
        Array.Copy(CountUnaccountedForCards, 1, virtualRemainingCards, 0, CARD_VECTOR_LENGTH);
        // Loop through the first hidden hand
        for(int h1 = 0; h1 < CARD_VECTOR_LENGTH; h1++) {
            virtualRemainingCards[h1] -= 1;
            // If there is only one opponent left, update SingleOpponentHandDistribution
            if(CurrentOpponentCount < 2) {
                HandDistribution[HiddenHands[0]][h1] = SingleOpponentHandDistribution[h1];
                CumulativeUpdateDeckDistribution(SingleOpponentHandDistribution[h1], virtualRemainingCards, ref DeckDistribution);
            } else {
                // Otherwise, enter the second loop
                for(int h2 = 0; h2 < CARD_VECTOR_LENGTH; h2++) {
                    virtualRemainingCards[h2] -= 1;
                    // If there are two opponents left, update TwoOpponentsHandsDistribution
                    if(CurrentOpponentCount < 3) {
                        HandDistribution[HiddenHands[0]][h1] += TwoOpponentsHandsDistribution[h1, h2];
                        HandDistribution[HiddenHands[1]][h2] += TwoOpponentsHandsDistribution[h1, h2];
                        CumulativeUpdateDeckDistribution(TwoOpponentsHandsDistribution[h1, h2], virtualRemainingCards, ref DeckDistribution);
                    } else {
                        // Otherwise, enter the final loop
                        for(int h3 = 0; h3 < CARD_VECTOR_LENGTH; h3++) {
                            HandDistribution[HiddenHands[0]][h1] += ThreeOpponentsHandsDistribution[h1, h2, h3];
                            HandDistribution[HiddenHands[1]][h2] += ThreeOpponentsHandsDistribution[h1, h2, h3];
                            HandDistribution[HiddenHands[2]][h3] += ThreeOpponentsHandsDistribution[h1, h2, h3];
                            // And update ThreeOpponentsHandsDistribution
                            virtualRemainingCards[h3] -= 1;
                            CumulativeUpdateDeckDistribution(ThreeOpponentsHandsDistribution[h1, h2, h3], virtualRemainingCards, ref DeckDistribution);
                            virtualRemainingCards[h3] += 1;
                        }
                    }
                    virtualRemainingCards[h2] += 1;
                }
            }
            virtualRemainingCards[h1] += 1;
        }
        // Normalize distributions
        if(TheDeck.CountCardsLeft > 0) {
            DeckDistribution.Renormalize();
        }
        for(int h = 0; h < CurrentOpponentCount; h++) {
            HandDistribution[HiddenHands[h]].Renormalize();
        }
    }

    // Convenience function for easier deck distribution calculation
    // Note that because it is CUMULATIVE, DeckDistributionBuffer contains intermediary results and may not be cleared!
    protected void CumulativeUpdateDeckDistribution(float Probability, int[] RemainingCards, ref Distribution1D DeckDistributionBuffer) {
        Debug.Assert(RemainingCards.Length == CARD_VECTOR_LENGTH);
        Debug.Assert(DeckDistributionBuffer.Length == CARD_VECTOR_LENGTH);
        if(Probability > 0) {
            int sum = AIUtil.SumUpArray(RemainingCards);
            for(int i = 0; i < CARD_VECTOR_LENGTH; i++) {
                DeckDistributionBuffer[i] += GetRemainingCardProb(RemainingCards[i], sum) * Probability;
            }
        }
    }

    // Another convenience function, for handling negative and zero-valued inputs
    protected float GetRemainingCardProb(int RemainingCardsOfThisType, int TotalRemainingCards) {
        if(TotalRemainingCards <= 0 || RemainingCardsOfThisType <= 0) {
            return 0;
        }
        return (float)RemainingCardsOfThisType / TotalRemainingCards;
    }

    // Convenience function to retrieve hidden hand indices given the sitting order
    protected int GetHiddenHandIndex(int SittingOrder) {
        int result = Array.IndexOf(HiddenHands, SittingOrder);
        Debug.AssertFormat(result < CurrentOpponentCount, "Sitting order {0:D} is not among the currently in-game opponents! Hand index: {1:D}", SittingOrder, result);
        return result;
    }

    /***********************************************
	 * The rest are public functions for queries.  *
	 ***********************************************/
    public float GetDeckProbability(int CardValue) {
        return DeckDistribution[CardValue - 1];
    }

    public float GetHandProbability(int PlayerSittingOrder, int CardValue) {
        return HandDistribution[PlayerSittingOrder][CardValue - 1];
    }

    public int GetOpponentBeliefAboutMyHand(int PlayerSittingOrder) {
        Debug.Assert(PlayerSittingOrder != MySittingOrder);
        return PlayerKnowsThatMyHandIs[PlayerSittingOrder];
    }

    public void SetOpponentBeliefAboutMyHand(int PlayerSittingOrder, int CardValue) {
        Debug.Assert(PlayerSittingOrder != MySittingOrder);
        Debug.Assert(CardValue >= CardController.VALUE_GUARD && CardValue <= CardController.VALUE_PRINCESS);
        PlayerKnowsThatMyHandIs[PlayerSittingOrder] = CardValue;
    }

    public void ResetOpponentsBeliefsAboutMyHand() {
        Array.Clear(PlayerKnowsThatMyHandIs, 0, PlayerCount);
    }

    public int CountTargetingsByOpponent(int PlayerSittingOrder) {
        Debug.Assert(PlayerSittingOrder != MySittingOrder);
        return PlayerHasTargetedMe[PlayerSittingOrder];
    }

    protected void BumpTargetingsByOpponent(int PlayerSittingOrder) {
        Debug.Assert(PlayerSittingOrder != MySittingOrder);
        PlayerHasTargetedMe[PlayerSittingOrder] += 1;
    }

    public int CountKnockoutsByOpponent(int PlayerSittingOrder) {
        Debug.Assert(PlayerSittingOrder != MySittingOrder);
        return PlayerHasKnockouts[PlayerSittingOrder];
    }

    protected void BumpKnockoutsByOpponent(int PlayerSittingOrder) {
        Debug.Assert(PlayerSittingOrder != MySittingOrder);
        PlayerHasKnockouts[PlayerSittingOrder] += 1;
    }

    public bool IsPlayerKnockedOut(int PlayerSittingOrder) {
        return PlayerIsKnockedOut[PlayerSittingOrder];
    }

    /// <summary>
    /// Writes current beliefs to the specified arrays.
    /// </summary>
    /// <param name="OutHands">Hand distributions.</param>
    /// <param name="OutDeck">Deck distribution.</param>
    public void MirrorCurrentBeliefs(ref float[][] OutHands, ref float[] OutDeck) {
        for(int c = 0; c < CARD_VECTOR_LENGTH; c++) {
            OutDeck[c + 1] = DeckDistribution[c];
            for(int p = 0; p < PlayerCount; p++) {
                OutHands[p][c + 1] = HandDistribution[p][c];
            }
        }
    }
}
