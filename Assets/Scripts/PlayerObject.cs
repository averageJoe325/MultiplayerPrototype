using System;
using RiptideNetworking;
using UnityEngine;

/// <summary>  The <see cref="WorldObject"/>s that represent players. </summary>
public class PlayerObject : WorldObject
{
    /// <summary> Stores the login info for the player. </summary>
    public LoginInfo LoginInfo { get; private set; }
    /// <summary> The Id of the onwer client. </summary>
    public ushort ClientId { get; private set; }
    /// <summary> The username. </summary>
    public string Username { get; private set; }

    /// <summary> Creates a new <see cref="PlayerObject"/>. </summary>
    /// <param name="clientId"> The id of the client that owns the <see cref="PlayerObject"/>. </param>
    /// <param name="username"> The username of the client that owns the <see cref="PlayerObject"/>. </param>
    /// <param name="position"> The position of the <see cref="PlayerObject"/>. </param>
    /// <param name="rotation"> The position of the <see cref="PlayerObject"/>. </param>
    /// <param name="health"> The amount of health the <see cref="PlayerObject"/> starts with. </param>
    /// <param name="statusEffects"> The status effects the <see cref="PlayerObject"/> has. </param>
    /// <param name="objects"> The objects stored within the inventory of the <see cref="PlayerObject"/>. </param>
    /// <param name="amounts"> The amount of each object stored within the inventory of the <see cref="PlayerObject"/>. </param>
    /// <param name="id"> The ID of the <see cref="PlayerObject"/>. Defaults to -1, which indicates that a new unique ID must be found. </param>
    /// <param name="loginInfo"> The login info of the player that owns the <see cref="PlayerObject"/></param>
    /// <exception cref="ArgumentException"></exception>
    public static PlayerObject Create(ushort clientId, string username, Vector3 position, Quaternion rotation, double health = double.PositiveInfinity, double[] statusEffects = null, ObjectType[] objects = null, int[] amounts = null, int id = -1, LoginInfo loginInfo = null)
    {
        PlayerObject player = (PlayerObject)Create(ObjectType.Player, position, rotation, health, statusEffects, objects, amounts, id);
        player.ClientId = clientId;
        player.Username = username;
        if (NetworkManager.Singleton.Server.IsRunning)
        {
            player.LoginInfo = loginInfo;
            SendPlayerCreate(clientId, username, player.transform.position, player.transform.rotation, player._id);
        }
        return player;
    }

    /// <summary> Sends the creation of a <see cref= "PlayerObject"/> to the clients. <para/>
    /// Message format: int, Vector3, Quaternion, int. </summary>
    /// /// <param name="ownerId"> The id of the client that owns the <see cref="PlayerObject"/>. </param>
    /// <param name="username"> The username of the client that owns the <see cref="PlayerObject"/>. </param>
    /// <param name = "position" > The position of the <see cref="GameObject"/> associated with the <see cref= "PlayerObject"/>. </param>
    /// <param name = "rotation" > The rotation of the <see cref="GameObject"/> associated with the <see cref= "PlayerObject"/>. </param>
    /// <param name="id"> The ID of the <see cref="PlayerObject"/>. </param>
    /// <param name="receiverId"> The ID of the <see cref="Client"/> that receives the message. Defaults to -1, indicating that all non-server clients will recieve the message. </param>
    public static void SendPlayerCreate(ushort ownerId, string username, Vector3 position, Quaternion rotation, int id, int receiverId = -1)
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.CreateWorldObject);
        message.AddInt((int)ObjectType.Player);
        message.AddUShort(ownerId);
        message.AddString(username);
        message.AddVector3(position);
        message.AddQuaternion(rotation);
        message.AddInt(id);
        if (receiverId == -1)
            NetworkManager.Singleton.Server.SendToAll(message, NetworkManager.Singleton.Client.Id);
        else
            NetworkManager.Singleton.Server.Send(message, (ushort)receiverId);
    }
}