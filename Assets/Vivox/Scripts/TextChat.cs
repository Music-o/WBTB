using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VivoxUnity;
using UnityEngine.UI;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
public class TextChat : MonoBehaviour
{
    private VoiceManager _voiceManager;
    //private const string LobbyChannelName = "testChannel";
    private ChannelId _GameChannel;
    private List<GameObject> _messageObjPool = new List<GameObject>();
    private ScrollRect _textChatScrollRect;

    private PlayerManager[] _playerManagers;
    private NetworkManagerHUDWBTB _networkManager;
    //private NetworkRoomPlayerWBTB[] _roomPlayer;
    //public string[] playerNameList = new string[6];

    public GameObject ChatContentObj;
    public GameObject MessageObject;
    public Button EnterButton;
    public InputField MessageInputField;

    private void Awake()
    {
        _textChatScrollRect = GetComponent<ScrollRect>();
        _voiceManager = VoiceManager.Instance;
        _networkManager = GameObject.Find("NetworkRoomManager").GetComponent<NetworkManagerHUDWBTB>();
        //if (_messageObjPool.Count > 0)
        //{

        //    ClearMessageObjectPool();
        //}
        ClearOutTextField();

        _voiceManager.OnParticipantAddedEvent += OnParticipantAdded;
        _voiceManager.OnTextMessageLogReceivedEvent += OnTextMessageLogReceivedEvent;

        EnterButton.onClick.AddListener(SubmitTextToVivox);
        MessageInputField.onEndEdit.AddListener((string text) => { EnterKeyOnTextField(); });

        if (_voiceManager.ActiveChannels.Count > 0)
        {
            _GameChannel = _voiceManager.ActiveChannels.FirstOrDefault(ac => ac.Channel.Name == _voiceManager.channelName).Key;
        }

        StartCoroutine(getPlayerManager());
        Debug.Log("textchat awake");
    }

    IEnumerator getPlayerManager()
    {
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "InGame");
        _playerManagers = new PlayerManager[6];
        //_roomPlayer = new NetworkRoomPlayerWBTB[6];
        int i = 0;
        foreach(var p in GameObject.FindGameObjectsWithTag("Player"))
        {
            _playerManagers[i] = p.GetComponent<PlayerManager>();
            i++;
        }
        //i = 0;
        //foreach (var p in GameObject.FindGameObjectsWithTag("PlayerList"))
        //{
        //    _roomPlayer[i] = p.GetComponent<NetworkRoomPlayerWBTB>();
        //    i++;
        //}
        //i = 0;
        //foreach(var p in _roomPlayer)
        //{
        //    playerNameList[i] = _roomPlayer[i].playerNameList[_roomPlayer[i].index];
        //    i++;
        //}
    }

    private void OnDestroy()
    {
        ClearMessageObjectPool();
        _voiceManager.OnParticipantAddedEvent -= OnParticipantAdded;
        _voiceManager.OnTextMessageLogReceivedEvent -= OnTextMessageLogReceivedEvent;
    }

    private void ClearMessageObjectPool()
    {
        for (int i = 0; i < _messageObjPool.Count; i++)
        {
            Destroy(_messageObjPool[i]);
        }
        _messageObjPool.Clear();
    }

    private void ClearOutTextField() // iput Field의 text를 지워줌
    {
        MessageInputField.text = string.Empty;
        MessageInputField.Select();
        MessageInputField.ActivateInputField();
    }


    private void EnterKeyOnTextField()  // text field에서 enter(return) key 누를시 전송되도록해주는 method
    {
        if (!Input.GetKeyDown(KeyCode.Return))
        {
            return;
        }
        SubmitTextToVivox();
    }
    private void SubmitTextToVivox() // vivox channel로 text를 전송
    {
        if (string.IsNullOrEmpty(MessageInputField.text))
        {
            return;
        }

        _voiceManager.SendTextMessage(MessageInputField.text, _GameChannel);
        ClearOutTextField();
    }

    public static string TruncateAtWord(string value, int length) // text 길이 오버했을때 자르는 method (안씀)
    {
        if (value == null || value.Length < length || value.IndexOf(" ", length) == -1)
            return value;

        return value.Substring(0, value.IndexOf(" ", length));
    }



    //public string[] TruncateWithPreservation(string s, int len)
    //{
    //    string[] words = s.Split(' ');
    //    string[] sections;

    //    StringBuilder sb = new StringBuilder();

    //    string currentString;

    //    foreach (string word in words)
    //    {
    //        if (sb.Length + word.Length > len)

    //            currentString = Strin;
    //            break;
    //        currentString += " ";
    //        currentString += word;
    //    }

    //    return sb.ToString();
    //}

    private IEnumerator SendScrollRectToBottom() // 스크롤바를 맨 아래로 보내줌 => 새로운 메시지가 들어오면 맨 아래를 보여주기 위함
    {
        yield return new WaitForEndOfFrame();

        // We need to wait for the end of the frame for this to be updated, otherwise it happens too quickly.
        _textChatScrollRect.normalizedPosition = new Vector2(0, 0);

        yield return null;
    }

    void OnParticipantAdded(string username, ChannelId channel, IParticipant participant)
    {
        if (_voiceManager.ActiveChannels.Count > 0)
        {
            _GameChannel = _voiceManager.ActiveChannels.FirstOrDefault().Channel; // 이 script에서 쓸 lobbychannel ID를 받아옴
        }
    }

    private void OnTextMessageLogReceivedEvent(string sender, IChannelTextMessage channelTextMessage) // text message가 vivox channel에 들어오는 event 발생에 대한 handler
    {
        if (!String.IsNullOrEmpty(channelTextMessage.ApplicationStanzaNamespace))
        {
            // If we find a message with an ApplicationStanzaNamespace we don't push that to the chat box.
            // Such messages denote opening/closing or requesting the open status of multiplayer matches.
            return;
        }
        var newMessageObj = Instantiate(MessageObject, ChatContentObj.transform);
        _messageObjPool.Add(newMessageObj);
        Text newMessageText = newMessageObj.GetComponent<Text>();

        if (SceneManager.GetActiveScene().name != "InGame")
        {
            if (channelTextMessage.FromSelf) // 내가 보낸것
            {
                newMessageText.alignment = TextAnchor.MiddleRight;
                newMessageText.text = string.Format($"<color=blue>{sender} :</color> {channelTextMessage.Message}");
                StartCoroutine(SendScrollRectToBottom());
            }
            else // 딴 사람이 보낸 것
            {
                newMessageText.alignment = TextAnchor.MiddleLeft;
                newMessageText.text = string.Format($"<color=green>{sender} </color> : {channelTextMessage.Message}");
                StartCoroutine(SendScrollRectToBottom());
            }
        }
        else
        {
            Image newMessageImg = newMessageObj.GetComponentInChildren<Image>();
            foreach (var p in _playerManagers) {
              if(_networkManager.PlayerNameList_[p.index] == channelTextMessage.Sender.DisplayName)
                {
                    newMessageImg.sprite = p.color_sprite[p.myColor];
                    break;
                }
            }
            if (channelTextMessage.FromSelf) // 내가 보낸것
            {
                newMessageImg.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                newMessageImg.rectTransform.anchorMin = new Vector2(1f, 0.5f);
                newMessageImg.rectTransform.anchoredPosition = new Vector2(-55f, 0f);

                newMessageText.alignment = TextAnchor.MiddleRight;
                newMessageText.text = string.Format($"{channelTextMessage.Message}       ");
                StartCoroutine(SendScrollRectToBottom());
            }
            else // 딴 사람이 보낸 것
            {
                newMessageImg.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                newMessageImg.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                newMessageImg.rectTransform.anchoredPosition = new Vector2(20f, 0f);
                newMessageText.alignment = TextAnchor.MiddleLeft;
                newMessageText.text = string.Format($"    {channelTextMessage.Message}");
                StartCoroutine(SendScrollRectToBottom());
            }
        }
    }

}
