﻿using System;
using System.Collections.Generic;
using System.IO;
using MLAPI.Components;
using MLAPI.Logging;
using MLAPI.Serialization;
using UnityEngine;

namespace MLAPI
{
    /// <summary>
    /// A component used to identify that a GameObject is networked
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkedObject", -99)]
    public sealed class NetworkedObject : MonoBehaviour
    {
        internal static readonly List<NetworkedBehaviour> NetworkedBehaviours = new List<NetworkedBehaviour>();

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(NetworkedPrefabName))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The NetworkedObject " + gameObject.name + " does not have a NetworkedPrefabName. It has been set to the gameObject name");
                NetworkedPrefabName = gameObject.name;
            }
        }

        /// <summary>
        /// Gets the unique ID of this object that is synced across the network
        /// </summary>
        public uint NetworkId { get; internal set; }
        /// <summary>
        /// Gets the clientId of the owner of this NetworkedObject
        /// </summary>
        public uint OwnerClientId
        {
            get
            {
                if (_ownerClientId == null)
					return NetworkingManager.singleton != null ? NetworkingManager.singleton.ServerClientId : 0;
                else
                    return _ownerClientId.Value;
            }
            internal set
            {
				if (NetworkingManager.singleton != null && value == NetworkingManager.singleton.ServerClientId)
                    _ownerClientId = null;
                else
                    _ownerClientId = value;
            }
        }
        private uint? _ownerClientId = null;
        /// <summary>
        /// The name of the NetworkedPrefab
        /// </summary>
        [Tooltip("The prefab name is the name that identifies this prefab. It has to not be the same as any other prefabs that are registered with the MLAPI and it has to be the same across projects if multiple projects are used.")]
        public string NetworkedPrefabName = string.Empty;
        /// <summary>
        /// The hash used to identify the NetworkedPrefab, a hash of the NetworkedPrefabName
        /// </summary>
        public ulong NetworkedPrefabHash => SpawnManager.GetPrefabHash(NetworkedPrefabName);
        /// <summary>
        /// Gets if this object is a player object
        /// </summary>
        public bool isPlayerObject { get; internal set; }
        /// <summary>
        /// Gets if this object is part of a pool
        /// </summary>
        public bool isPooledObject { get; internal set; }
        /// <summary>
        /// Gets the poolId this object is part of
        /// </summary>
        public ushort PoolId { get; internal set; }
        /// <summary>
        /// Gets if the object is the the personal clients player object
        /// </summary>
		public bool isLocalPlayer => NetworkingManager.singleton != null && isPlayerObject && OwnerClientId == NetworkingManager.singleton.LocalClientId;
		/// <summary>
		/// Gets if the object is owned by the local player or if the object is the local player object
		/// </summary>
		public bool isOwner => NetworkingManager.singleton != null && OwnerClientId == NetworkingManager.singleton.LocalClientId;
        /// <summary>
        /// Gets wheter or not the object is owned by anyone
        /// </summary>
		public bool isOwnedByServer => NetworkingManager.singleton != null && OwnerClientId == NetworkingManager.singleton.ServerClientId;
        /// <summary>
        /// Gets if the object has yet been spawned across the network
        /// </summary>
        public bool isSpawned { get; internal set; }
        internal bool? destroyWithScene = null;

        /// <summary>
        /// When enabled this gameobject will not be spawned on the client until the scene it was originally spawned inside at the server is fully loaded on the client.
        /// </summary>
        [Tooltip("When enabled this gameobject will not be spawned on the client until the scene it was originally spawned inside at the server is fully loaded on the client.")]
        public bool SceneDelayedSpawn = false;

        internal uint sceneSpawnedInIndex = 0;

        private void OnDestroy()
        {
            if (NetworkingManager.singleton != null)
                SpawnManager.OnDestroyObject(NetworkId, false);
        }

        /// <summary>
        /// Spawns this GameObject across the network. Can only be called from the Server
        /// </summary>
        /// <param name="spawnPayload">The writer containing the spawn payload</param>
        /// <param name="destroyWithScene">Should the object be destroyd when the scene is changed</param>
        public void Spawn(Stream spawnPayload = null, bool destroyWithScene = false)
        {
            SpawnManager.SpawnObject(this, null, spawnPayload, destroyWithScene);
        }

        /// <summary>
        /// Unspawns this GameObject and destroys it for other clients. This should be used if the object should be kept on the server
        /// </summary>
        public void UnSpawn()
        {
            SpawnManager.UnSpawnObject(this);
        }

        /// <summary>
        /// Spawns an object across the network with a given owner. Can only be called from server
        /// </summary>
        /// <param name="clientId">The clientId to own the object</param>
        /// <param name="spawnPayload">The writer containing the spawn payload</param>
        /// <param name="destroyWithScene">Should the object be destroyd when the scene is changed</param>
        public void SpawnWithOwnership(uint clientId, Stream spawnPayload = null, bool destroyWithScene = false)
        {
            SpawnManager.SpawnObject(this, clientId, spawnPayload, destroyWithScene);
        }

        /// <summary>
        /// Spawns an object across the network and makes it the player object for the given client
        /// </summary>
        /// <param name="clientId">The clientId whos player object this is</param>
        /// <param name="spawnPayload">The writer containing the spawn payload</param>
        public void SpawnAsPlayerObject(uint clientId, Stream spawnPayload = null)
        {
            SpawnManager.SpawnPlayerObject(this, clientId, spawnPayload);
        }

        /// <summary>
        /// Removes all ownership of an object from any client. Can only be called from server
        /// </summary>
        public void RemoveOwnership()
        {
            SpawnManager.RemoveOwnership(NetworkId);
        }
        /// <summary>
        /// Changes the owner of the object. Can only be called from server
        /// </summary>
        /// <param name="newOwnerClientId">The new owner clientId</param>
        public void ChangeOwnership(uint newOwnerClientId)
        {
            SpawnManager.ChangeOwnership(NetworkId, newOwnerClientId);
        }

        internal void InvokeBehaviourOnLostOwnership()
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                childNetworkedBehaviours[i].OnLostOwnership();
            }
        }

        internal void InvokeBehaviourOnGainedOwnership()
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                childNetworkedBehaviours[i].OnGainedOwnership();
            }
        }

        internal void InvokeBehaviourNetworkSpawn(Stream stream)
        {
            for (int i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                //We check if we are it's networkedObject owner incase a networkedObject exists as a child of our networkedObject.
                if(!childNetworkedBehaviours[i].networkedStartInvoked)
                {
                    childNetworkedBehaviours[i].InternalNetworkStart();
                    childNetworkedBehaviours[i].NetworkStart(stream);
                    childNetworkedBehaviours[i].networkedStartInvoked = true;
                }
            }
        }

        private List<NetworkedBehaviour> _childNetworkedBehaviours;
        internal List<NetworkedBehaviour> childNetworkedBehaviours
        {
            get
            {
                if(_childNetworkedBehaviours == null)
                {
                    _childNetworkedBehaviours = new List<NetworkedBehaviour>();
                    NetworkedBehaviour[] behaviours = GetComponentsInChildren<NetworkedBehaviour>();
                    for (int i = 0; i < behaviours.Length; i++)
                    {
                        if (behaviours[i].networkedObject == this)
                            _childNetworkedBehaviours.Add(behaviours[i]);
                    }
                }
                return _childNetworkedBehaviours;
            }
        }

        internal static void NetworkedVarPrepareSend()
        {
            for (int i = 0; i < NetworkedBehaviours.Count; i++)
            {
                NetworkedBehaviours[i].NetworkedVarUpdate();
            }
        }
        
        internal void WriteNetworkedVarData(Stream stream, uint clientId)
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                for (int i = 0; i < childNetworkedBehaviours.Count; i++)
                {
                    childNetworkedBehaviours[i].NetworkedVarInit();
                    NetworkedBehaviour.WriteNetworkedVarData(childNetworkedBehaviours[i].networkedVarFields, writer, stream, clientId);
                }
            }
        }

        internal void SetNetworkedVarData(Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                for (int i = 0; i < childNetworkedBehaviours.Count; i++)
                {
                    childNetworkedBehaviours[i].NetworkedVarInit();
                    NetworkedBehaviour.SetNetworkedVarData(childNetworkedBehaviours[i].networkedVarFields, reader, stream);
                }
            }
        }

        internal ushort GetOrderIndex(NetworkedBehaviour instance)
        {
            for (ushort i = 0; i < childNetworkedBehaviours.Count; i++)
            {
                if (childNetworkedBehaviours[i] == instance)
                    return i;
            }
            return 0;
        }

        internal NetworkedBehaviour GetBehaviourAtOrderIndex(ushort index)
        {
            if (index >= childNetworkedBehaviours.Count)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Behaviour index was out of bounds. Did you mess up the order of your NetworkedBehaviours?");
                return null;
            }
            return childNetworkedBehaviours[index];
        }
    }
}
