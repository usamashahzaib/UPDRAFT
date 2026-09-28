using UnityEngine;
using Updraft.Player;

namespace Updraft.Map
{
    public class SpawnExitBarrier : MonoBehaviour
    {
        public int OwnerTeam;
        public static void ConfigurePlayer(PlayerController player)
        {
            var character = player.GetComponent<CharacterController>();
            foreach (var exit in FindObjectsByType<SpawnExitBarrier>(FindObjectsSortMode.None))
                Physics.IgnoreCollision(exit.GetComponent<Collider>(), character, player.TeamId == exit.OwnerTeam);
        }
    }
}
