/*
 * author: mark joshwel
 * date: 13/5/2026
 * description: generic finite state machine for composition
 *              into consumer unity game objects and entities
 */

using System;

namespace majo.Core
{
    /// <summary>
    ///   <para>
    ///     small enum-based finite state machine for runtime scripts
    ///   </para>
    /// </summary>
    /// <typeparam name="TState">enum type used for the state machine</typeparam>
    /// <example>
    ///   <code>
    ///   using marco;
    ///   using UnityEngine;
    ///  
    ///   public enum EnemyState
    ///   {
    ///       Idle,
    ///       Stunned,
    ///       Dead
    ///   }
    ///  
    ///   public sealed class EnemyController : MonoBehaviour
    ///   {
    ///       [SerializeField] private int health = 3;
    ///
    ///       private FiniteStateMachine&lt;EnemyState&gt; state;
    ///
    ///       private void Awake()
    ///       {
    ///           state = new FiniteStateMachine&lt;EnemyState&gt;(EnemyState.Idle);
    ///           state.OnStateChanged += OnStateChanged;
    ///       }
    ///  
    ///       public void TakeHit(int damage)
    ///       {
    ///           if (state.Is(EnemyState.Dead)) return;
    ///  
    ///           health -= damage;
    ///  
    ///           if (health &lt;= 0)
    ///               state.ChangeState(EnemyState.Dead);
    ///           else
    ///               state.ChangeState(EnemyState.Stunned);
    ///       }
    ///  
    ///       public void Recover()
    ///       {
    ///           if (state.Is(EnemyState.Stunned))
    ///               state.ChangeState(EnemyState.Idle);
    ///       }
    ///  
    ///       private void OnStateChanged(EnemyState oldState, EnemyState newState)
    ///       {
    ///           Debug.Log($"enemy state: {oldState} -> {newState}");
    ///       }
    ///   }
    ///   </code>
    /// </example>
    public sealed class FiniteStateMachine<TState> where TState : Enum
    {
        /// <summary>
        ///     current state
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public TState State { get; private set; }

        /// <summary>
        ///     previous state before the latest transition
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public TState PreviousState { get; private set; }

        /// <summary>
        ///     fired after the state changes. arguments are old state, then new state
        /// </summary>
        public event Action<TState, TState> OnStateChanged;

        /// <summary>
        ///     creates a finite state machine with an initial state
        /// </summary>
        /// <param name="initialState">starting state</param>
        public FiniteStateMachine(TState initialState)
        {
            State = initialState;
            PreviousState = initialState;
        }

        /// <summary>
        ///     changes to a new state and notifies listeners
        /// </summary>
        /// <param name="newState">state to switch to</param>
        public void ChangeState(TState newState)
        {
            if (Equals(State, newState)) return;

            var oldState = State;
            PreviousState = oldState;
            State = newState;

            OnStateChanged?.Invoke(oldState, newState);
        }

        /// <summary>
        ///     checks whether the current state matches the expected state
        /// </summary>
        /// <param name="expectedState">state to compare against</param>
        /// <returns>true if the current state matches</returns>
        public bool Is(TState expectedState)
        {
            return Equals(State, expectedState);
        }
    }
}