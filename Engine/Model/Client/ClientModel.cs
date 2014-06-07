﻿using Engine.Model.Entities;
using Engine.Network;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace Engine.Model.Client
{
  public class ClientModel
  {
    #region static model
    private static ClientModel model;

    /// <summary>
    /// Клиентский API
    /// </summary>
    public static IClientAPI API { get; set; }

    /// <summary>
    /// Клиентское соединение
    /// </summary>
    public static AsyncClient Client { get; private set; }

    /// <summary>
    /// Исользовать только с конструкцией using(var context = ClientModel.Get()) { ... }
    /// </summary>
    /// <returns>Возвращает и блокирует модель.</returns>
    public static ClientContext Get()
    {
      if (Interlocked.CompareExchange(ref model, null, null) == null)
        throw new ArgumentException("model do not inited yet");

      return new ClientContext(model);
    }
    #endregion

    #region events
    /// <summary>
    /// Событие происходит при обновлении списка подключенных к серверу клиентов.
    /// </summary>
    public static event EventHandler<RoomEventArgs> RoomRefreshed;

    internal static void OnRoomRefreshed(object sender, RoomEventArgs args) 
    {
      Raise(ref RoomRefreshed, sender, args); 
    }

    /// <summary>
    /// Событие происходит при подключении клиента к серверу.
    /// </summary>
    public static event EventHandler<ConnectEventArgs> Connected;

    internal static void OnConnected(object sender, ConnectEventArgs args)
    { 
      Raise(ref Connected, sender, args); 
    }

    /// <summary>
    /// Событие происходит при полученни ответа от сервера, о регистрации.
    /// </summary>
    public static event EventHandler<RegistrationEventArgs> ReceiveRegistrationResponse;

    internal static void OnReceiveRegistrationResponse(object sender, RegistrationEventArgs args)
    { 
      Raise(ref ReceiveRegistrationResponse, sender, args); 
    }

    /// <summary>
    /// Событие происходит при полученнии сообщения от сервера.
    /// </summary>
    public static event EventHandler<ReceiveMessageEventArgs> ReceiveMessage;

    internal static void OnReceiveMessage(object sender, ReceiveMessageEventArgs args)
    { 
      Raise(ref ReceiveMessage, sender, args); 
    }

    internal static void OnSystemMessage(string message)
    {
      OnReceiveMessage(null, new ReceiveMessageEventArgs { Type = MessageType.System, Message = message });
    }

    /// <summary>
    /// Событие происходит при любой асинхронной ошибке.
    /// </summary>
    public static event EventHandler<AsyncErrorEventArgs> AsyncError;

    internal static void OnAsyncError(object sender, AsyncErrorEventArgs args)
    { 
      Raise(ref AsyncError, sender, args); 
    }

    /// <summary>
    /// Событие происходит при открытии комнаты клиентом. Или когда клиента пригласили в комнату.
    /// </summary>
    public static event EventHandler<RoomEventArgs> RoomOpened;

    internal static void OnRoomOpened(object sender, RoomEventArgs args)
    { 
      Raise(ref RoomOpened, sender, args); 
    }

    /// <summary>
    /// Событие происходит при закрытии комнаты клиентом, когда клиента кикают из комнаты.
    /// </summary>
    public static event EventHandler<RoomEventArgs> RoomClosed;

    internal static void OnRoomClosed(object sender, RoomEventArgs args) 
    {
      Raise(ref RoomClosed, sender, args); 
    }

    /// <summary>
    /// Событие происходит при получении части файла, а также при завершении загрузки файла.
    /// </summary>
    public static event EventHandler<FileDownloadEventArgs> DownloadProgress;

    internal static void OnDownloadProgress(object sender, FileDownloadEventArgs args) 
    { 
      Raise(ref DownloadProgress, sender, args); 
    }

    /// <summary>
    /// Происходит при удалении выложенного файла.
    /// </summary>
    public static event EventHandler<FileDownloadEventArgs> PostedFileDeleted;

    internal static void OnPostedFileDeleted(object sender, FileDownloadEventArgs args) 
    { 
      Raise(ref PostedFileDeleted, sender, args); 
    }

    private static void Raise<T>(ref EventHandler<T> eventHandler, object sender, T args) where T: EventArgs
    {
      var temp = Interlocked.CompareExchange(ref eventHandler, null, null);

      if (temp != null)
        temp(sender, args);
    }

    #endregion events

    #region properties
    public User User { get; private set; }
    public List<DownloadingFile> DownloadingFiles { get; private set; }
    public List<PostedFile> PostedFiles { get; private set; }
    #endregion

    #region conctructor
    public ClientModel()
    {
      DownloadingFiles = new List<DownloadingFile>();
      PostedFiles = new List<PostedFile>();
    }
    #endregion

    #region static methods
    public static bool IsInited
    {
      get
      {
        return Interlocked.CompareExchange(ref model, null, null) != null;
      }
    }

    public static void Init(string nick, Color nickColor)
    {
      if (Interlocked.CompareExchange(ref model, new ClientModel(), null) != null)
        throw new InvalidOperationException("model already inited");

      using (var client = Get())
      {
        model.User = new User(nick);
        model.User.NickColor = nickColor;
      }

      // API установится автоматически при подключении к серверу (согласно версии на сервере)
      Client = new AsyncClient(nick);
    }

    public static void Reset()
    {
      if (Interlocked.Exchange(ref model, null) == null)
        throw new InvalidOperationException("model not yet inited");

      if (Client != null)
      {
        Client.Dispose();
        Client = null;
      }

      API = null;
    }
    #endregion
  }
}
