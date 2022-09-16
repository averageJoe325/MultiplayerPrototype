using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RiptideNetworking;
using UnityEngine;

/// <summary> The type of a world object. </summary>
public enum ObjectType
{
    None,
    Player,
    Ground,
}

/// <summary> The type of a status effect. </summary>
public enum EffectType
{
    Invulnerable,
}

/// <summary> An object that interacts with the world and must remain synced across the <see cref="Server"/> and the <see cref="Client"/>s. </summary>
public class WorldObject : MonoBehaviour
{
    private const int STATUS_EFFECT_COUNT = 1;
    private static List<int> s_freeIds = new();
    private static int s_greatestId = -1;
    protected int _id;
    private Inventory _inventory = new(0);


    public static Dictionary<int, WorldObject> WorldObjectDict { get; private set; } = new();
    public ObjectType Type { get; private set; }
    public double Health { get; private set; } = double.PositiveInfinity;
    public double[] StatusEffects { get; private set; } = new double[STATUS_EFFECT_COUNT];
    public ObjectType[] Objects => _inventory.Objects;
    public int[] Amounts => _inventory.Amounts;

    // Tells the server to send the position and rotation every frame.
    protected virtual void FixedUpdate()
    {
        if (!NetworkManager.Singleton.Server.IsRunning)
            return;
        CheckForDeath();
        UpdateStatusEffects();
        SendMoveAndRotate();
        SendHealth();
        SendStatusEffects();
        SendInventory();
    }

    #region Creation and Destruction
    /// <summary> Creates a new <see cref="WorldObject"/>. </summary>
    /// <param name="type"> The type of <see cref="WorldObject"/>. </param>
    /// <param name="position"> The position of the <see cref="WorldObject"/>. </param>
    /// <param name="rotation"> The position of the <see cref="WorldObject"/>. </param>
    /// <param name="health"> The amount of health the <see cref="WorldObject"/> starts with. </param>
    /// <param name="statusEffects"> The status effects the <see cref="WorldObject"/> has. </param>
    /// <param name="objects"> The objects stored within the <see cref="Inventory"/> of the <see cref="WorldObject"/>. </param>
    /// <param name="amounts"> The amount of each object stored within the <see cref="Inventory"/> of the <see cref="WorldObject"/>. </param>
    /// <param name="id"> The ID of the <see cref="WorldObject"/>. Defaults to -1, which indicates that a new unique ID must be found. </param>
    /// <exception cref="ArgumentException"></exception>
    public static WorldObject Create(ObjectType type, Vector3 position, Quaternion rotation, double health = double.PositiveInfinity, double[] statusEffects = null, ObjectType[] objects = null, int[] amounts = null, int id = -1)
    {
        if (type == ObjectType.None)
            throw new ArgumentException("Can not create a World Object of type \"None\".");
        if (id == -1 && !NetworkManager.Singleton.Server.IsRunning)
            throw new ArgumentException("Can not create new IDs client side.");
        if (id != -1 && id <= s_greatestId && !s_freeIds.Contains(id))
            throw new ArgumentException("Can not create a World Object for an already allocated id.");
        if (id == -1)
        {
            if (s_freeIds.Count > 0)
                id = s_freeIds[0];
            else
                id = s_greatestId + 1;
        }
        WorldObject worldObject = Instantiate(GameManager.Singleton.WorldObjectPrefabs[(int)type], position, rotation, GameManager.Singleton.World.transform).GetComponent<WorldObject>();
        worldObject.Type = type;
        worldObject._id = id;
        worldObject.Initialize(health, statusEffects, objects, amounts);
        WorldObjectDict.Add(id, worldObject);
        if (NetworkManager.Singleton.Server.IsRunning)
        {
            s_freeIds.Remove(worldObject._id);
            if (worldObject._id > s_greatestId)
                s_greatestId = worldObject._id;
            if (type != ObjectType.Player)
                SendCreate(type, position, rotation, id);
        }
        return worldObject;
    }

    // Initialize the values of the world object.
    private void Initialize(double health, double[] statusEffects, ObjectType[] objects, int[] amounts)
    {
        if (health != double.PositiveInfinity)
            Health = health;
        if (statusEffects != null)
            StatusEffects = statusEffects;
        if (objects != null && amounts != null)
            _inventory = new(objects, amounts);
    }

    /// <summary> Destroys the <see cref="GameObject"/> associated with this instance. </summary>
    public void DestroyWorldObject()
    {
        if (NetworkManager.Singleton.Server.IsRunning)
        {
            if (_id == s_greatestId)
            {
                s_greatestId--;
                while (s_freeIds.Contains(s_greatestId))
                {
                    s_freeIds.Remove(s_greatestId);
                    s_greatestId--;
                }
            }
            else
                s_freeIds.Add(_id);
            SendDestroy();
        }
        WorldObjectDict.Remove(_id);
        Destroy(gameObject);
    }

    /// <summary> Sends the creation of a <see cref= "WorldObject"/> to the clients. <para/>
    /// Message format: int, Vector3, Quaternion, int. </summary>
    /// <param name = "type" > The type of <see cref= "WorldObject"/>.</param>
    /// <param name = "position" > The position of the <see cref="GameObject"/> associated with the <see cref= "WorldObject"/>. </param>
    /// <param name = "rotation" > The rotation of the <see cref="GameObject"/> associated with the <see cref= "WorldObject"/>. </param>
    /// <param name="id"> The ID of the <see cref="WorldObject"/>. </param>
    /// <param name="clientId"> The ID of the <see cref="Client"/>. Defaults to -1, indicating that all non-server clients will recieve the message. </param>
    public static void SendCreate(ObjectType type, Vector3 position, Quaternion rotation, int id, int clientId = -1)
    {
        if (type == ObjectType.None)
            throw new ArgumentException("Can not create a World Object of type \"None\".");
        if (type == ObjectType.Player)
            throw new ArgumentException("Can not send the creation a World Object of type \"Player\". Please use PlayerObject.SendCreate.");
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.CreateWorldObject);
        message.AddInt((int)type);
        message.AddVector3(position);
        message.AddQuaternion(rotation);
        message.AddInt(id);
        if (clientId == -1)
            NetworkManager.Singleton.Server.SendToAll(message, NetworkManager.Singleton.Client.Id);
        else
            NetworkManager.Singleton.Server.Send(message, (ushort)clientId);
    }

    // Handles the messages regarding the creation of a world object.
    // Message format: int, Vector3, Quaternion, int.
    [MessageHandler((ushort)ServerToClientId.CreateWorldObject)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Message handlers are not called in the script")]
    private static void GetCreate(Message message)
    {
        ObjectType type = (ObjectType)message.GetInt();
        if (type == ObjectType.Player)
            PlayerObject.Create(message.GetUShort(), message.GetString(), message.GetVector3(), message.GetQuaternion(), id: message.GetInt());
        else
            Create(type, message.GetVector3(), message.GetQuaternion(), id: message.GetInt());
    }

    // Sends the destruction of a world object to the clients.
    // Message format: int.
    private void SendDestroy()
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.DestroyWorldObject);
        message.AddInt(_id);
        NetworkManager.Singleton.Server.SendToAll(message, NetworkManager.Singleton.Client.Id);
    }

    // Handles the messages regarding the destruction of a world object.
    // Message format: int.
    [MessageHandler((ushort)ServerToClientId.DestroyWorldObject)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Message handlers are not called in the script")]
    private static void GetDestroy(Message message)
    {
        int id = message.GetInt();
        if (!WorldObjectDict.ContainsKey(id))
            return;
        WorldObjectDict[id].DestroyWorldObject();
    }
    #endregion


    #region Movement and Rotation
    // Sends the position and rotation of the game object associated with the world object to the clients.
    // Message format: int, Vector3, Quaternion.
    protected virtual void SendMoveAndRotate()
    {
        Message message = Message.Create(MessageSendMode.unreliable, ServerToClientId.MoveAndRotateWorldObject);
        message.AddInt(_id);
        message.AddVector3(transform.position);
        message.AddQuaternion(transform.rotation);
        NetworkManager.Singleton.Server.SendToAll(message, NetworkManager.Singleton.Client.Id);
    }

    // Handles the messages regarding the position and rotation of the game object associated with the world object.
    // Message format: int, Vector3, Quaternion.
    [MessageHandler((ushort)ServerToClientId.MoveAndRotateWorldObject)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Message handlers are not called in the script.")]
    private static void GetMoveAndRotate(Message message)
    {
        int id = message.GetInt();
        if (!WorldObjectDict.ContainsKey(id))
            return;
        Transform worldObjectTransform = WorldObjectDict[id].transform;
        worldObjectTransform.position = message.GetVector3();
        worldObjectTransform.rotation = message.GetQuaternion();
    }
    #endregion


    #region Health
    /// <summary> Reduces health of the world object. </summary>
    /// <param name="damage"> The amount the health is reduced. </param>
    public void TakeDamage(float damage)
    {
        if (!NetworkManager.Singleton.Server.IsRunning)
            return;
        Health -= damage;
    }

    // Checks if health is nonpositive and destorys the world object in that case.
    private void CheckForDeath()
    {
        if (Health <= 0)
            DestroyWorldObject();
    }

    // Sends the health of the world object.
    // Message format: int, double.
    private void SendHealth()
    {
        Message message = Message.Create(MessageSendMode.unreliable, ServerToClientId.UpdateHealthWorldObject);
        message.AddInt(_id);
        message.AddDouble(Health);
        NetworkManager.Singleton.Server.SendToAll(message, NetworkManager.Singleton.Client.Id);
    }


    // Handles the messsage regarding the health of the world object.
    // Message format: int, double.
    [MessageHandler((ushort)ServerToClientId.UpdateHealthWorldObject)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Message handlers are not called in the script.")]
    private static void GetHealth(Message message)
    {
        int id = message.GetInt();
        if (!WorldObjectDict.ContainsKey(id))
            return;
        WorldObjectDict[id].Health = message.GetDouble();
    }
    #endregion


    #region Status Effects
    /// <summary> Gives the <see cref="WorldObject"/> a status effect. </summary>
    /// <param name="type"> The type of effect. </param>
    /// <param name="length"> The length of time the effect lasts. </param>
    /// <param name="isAdditive"> Whether or not the effect is additive. </param>
    public void AddStatusEffect(EffectType type, double length, bool isAdditive = true)
    {
        if (!NetworkManager.Singleton.Server.IsRunning)
            return;
        if (isAdditive)
            StatusEffects[(int)type] += length;
        else
            StatusEffects[(int)type] = Math.Max(StatusEffects[(int)type], length);
    }

    // Decreases the values of the positive elements of the status effects.
    private void UpdateStatusEffects()
    {
        for (int i = 0; i < StatusEffects.Length; i++)
            StatusEffects[i] -= StatusEffects[i] > 0 ? Time.fixedDeltaTime : 0;
    }

    // Sends the status effects of the world object.
    // Message format: int, doubles.
    private void SendStatusEffects()
    {
        Message message = Message.Create(MessageSendMode.unreliable, ServerToClientId.UpdateStatusEffectsWorldObject);
        message.AddInt(_id);
        message.AddDoubles(StatusEffects, false);
        NetworkManager.Singleton.Server.SendToAll(message, NetworkManager.Singleton.Client.Id);
    }

    // Handles the message regarding the status effects of the world object.
    // Message format: int, doubles.
    [MessageHandler((ushort)ServerToClientId.UpdateStatusEffectsWorldObject)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Message handlers are not called in the script.")]
    private static void GetStatusEffects(Message message)
    {
        int id = message.GetInt();
        if (!WorldObjectDict.ContainsKey(id))
            return;
        WorldObjectDict[id].StatusEffects = message.GetDoubles(STATUS_EFFECT_COUNT);
    }
    #endregion


    #region Inventories
    // A collection of world objects to be stored.
    private class Inventory
    {
        private const int MAX_STACK_SIZE = 50;
        // The types of world objects in the inventory.
        public ObjectType[] Objects { get; private set; }
        // The amount of each world object type in the inventory.
        public int[] Amounts { get; private set; }

        // The size of the inventory.
        public int Size => Objects.Length;

        // Makes an inventory of the given size or with given arrays.
        public Inventory(int size)
        {
            Objects = new ObjectType[size];
            Amounts = new int[size];
        }
        public Inventory(ObjectType[] objects, int[] amounts)
        {
            Objects = objects;
            Amounts = amounts;
        }

        // Tries to add a world object to the inventory, returns whether or not it was successful.
        public bool Add(ObjectType type)
        {
            if (type == ObjectType.None)
                throw new ArgumentException("Can not add World Object of type \"None\" to inventory.");
            List<int> indices = Objects.IndicesOf(type);
            foreach (int i in indices)
            {
                if (Amounts[i] < MAX_STACK_SIZE)
                {
                    Amounts[i]++;
                    return true;
                }
            }
            int index = Objects.IndexOf(ObjectType.None);
            if (index != -1)
            {
                Objects[index] = type;
                Amounts[index]++;
                return true;
            }
            return false;
        }

        // Tries to remove a world object to the inventory, returns whether or not it was successful.
        public bool Remove(ObjectType type)
        {
            if (type == ObjectType.None)
                throw new ArgumentException("Can not remove World Object of type \"None\" to inventory.");
            int index = Objects.LastIndexOf(type);
            if (index != -1)
            {
                Amounts[index]--;
                if (Amounts[index] == 0)
                    Objects[index] = ObjectType.None;
                return true;
            }
            return false;
        }
    }

    // Sends the inventory of the world object.
    // Message format: int, int, ints, ints.
    private void SendInventory()
    {
        Message message = Message.Create(MessageSendMode.unreliable, ServerToClientId.UpdateInventoryWorldObject);
        message.AddInt(_id);
        message.AddInt(_inventory.Size);
        message.AddInts(Array.ConvertAll(_inventory.Objects, (ObjectType type) => (int)type), false);
        message.AddInts(_inventory.Amounts, false);
        NetworkManager.Singleton.Server.SendToAll(message, NetworkManager.Singleton.Client.Id);
    }

    // Handles the message regarding the inventory of the world object.
    // Message format: int, int, ints, ints.
    [MessageHandler((ushort)ServerToClientId.UpdateInventoryWorldObject)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Message handlers are not called in the script.")]
    private static void GetInventory(Message message)
    {
        int id = message.GetInt();
        if (!WorldObjectDict.ContainsKey(id))
            return;
        int size = message.GetInt();
        ObjectType[] objects = Array.ConvertAll(message.GetInts(size), (int input) => (ObjectType)input);
        int[] amounts = message.GetInts(size);
        WorldObjectDict[id]._inventory = new Inventory(objects, amounts);
    }
    #endregion
}