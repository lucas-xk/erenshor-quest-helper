using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace questhelper
{
    [BepInPlugin("lucasxk.erenshor.questhelper", "Quest Helper", "2.2.0")]
    public class QuestHelper : BaseUnityPlugin
    {
        private Texture2D _texYellowExclamation;
        private Texture2D _texYellowQuestion;
        private Texture2D _texBlueExclamation;
        private Texture2D _texBlueQuestion;
        private Texture2D _texGreyQuestion;

        public const float QuestMarkerRadius = 70f;

        private float _checkInterval = 1f;
        private float _nextCheckTime = 0f;

        private void Awake()
        {
            LoadTextures();
        }

        private void Update()
        {
            if (Time.time < _nextCheckTime)
                return;

            _nextCheckTime = Time.time + _checkInterval;

            CheckQuestMarkers();
        }

        private void CheckQuestMarkers()
        {
            if (GameData.PlayerControl == null || GameData.InCharSelect) return;

            if (_texYellowExclamation == null || _texYellowQuestion == null)
                LoadTextures();

            Collider[] hitColliders = Physics.OverlapSphere(
                GameData.PlayerControl.transform.position,
                QuestMarkerRadius
            );

            foreach (var collider in hitColliders)
            {
                Character character = collider.GetComponent<Character>();
                if (character == null || !character.isNPC) continue;

                Texture2D finalTexture = null;
                QuestMarker.Type finalType = QuestMarker.Type.None;

                // 1 - check turn in quests
                var questManager = character.GetComponent<QuestManager>();
                bool foundBlueTurnIn = false;

                // fix 1 - check if questManager.NPCQuests != null
                if (questManager != null && questManager.NPCQuests != null)
                {
                    foreach (Quest quest in questManager.NPCQuests)
                    {
                        // fix 2 - if quest is in a null list
                        if (quest == null) continue;

                        if (GameData.HasQuest.Contains(quest.DBName) && !GameData.CompletedQuests.Contains(quest.DBName))
                        {
                            bool hasItems = CheckPlayerHasItems(quest);

                            if (hasItems)
                            {
                                // if non-repeatable (yellow), priority above all
                                if (!quest.repeatable)
                                {
                                    finalTexture = _texYellowQuestion;
                                    finalType = QuestMarker.Type.TurnIn;
                                    goto ApplyMarker; // end of loop = yellow
                                }
                                else
                                {
                                    // if repeatable (blue), find blue flag
                                    foundBlueTurnIn = true;
                                }
                            }
                            else
                            {
                                // on going quests (gray)
                                // only if didn't find blue turn in (because turn in > on going)
                                if (finalType == QuestMarker.Type.None && !foundBlueTurnIn)
                                {
                                    finalTexture = _texGreyQuestion;
                                    finalType = QuestMarker.Type.Incomplete;
                                }
                            }
                        }
                    }
                }

                // if end of loop and find blue flag == true (and none yellow), use blue
                if (foundBlueTurnIn)
                {
                    finalTexture = _texBlueQuestion;
                    finalType = QuestMarker.Type.TurnIn;
                    goto ApplyMarker;
                }

                // 2 - check disponibility (if there is no turn in)
                if (finalType != QuestMarker.Type.TurnIn)
                {
                    var availType = GetQuestAvailabilityType(character);

                    if (availType != QuestMarker.Type.None)
                    {
                        if (availType == QuestMarker.Type.RepeatableNew)
                        {
                            // repeatable (first time quest) -> blue !
                            finalTexture = _texBlueExclamation;
                            finalType = QuestMarker.Type.Available;
                        }
                        else if (availType == QuestMarker.Type.Available)
                        {
                            // non-repeatable (first time quest) -> yellow !
                            finalTexture = _texYellowExclamation;
                            finalType = QuestMarker.Type.Available;
                        }
                        else if (availType == QuestMarker.Type.RepeatableDone)
                        {
                            // repeatable (already done) -> blue ?
                            finalTexture = _texBlueQuestion;
                            finalType = QuestMarker.Type.Repeatable;
                        }
                    }
                }

                ApplyMarker:
                if (finalType != QuestMarker.Type.None && finalTexture != null)
                {
                    AttachMarkerToCharacter(character, finalTexture, finalType);
                }
                else
                {
                    var marker = character.transform.Find("QuestMarkerWorldUI");
                    if (marker != null) Destroy(marker.gameObject);
                }
            }
        }

        private void LoadTextures()
        {
            _texYellowExclamation = LoadImageFromResource("questhelper.Resources.quest-available.png");
            _texYellowQuestion = LoadImageFromResource("questhelper.Resources.quest-complete.png");

            if (_texYellowExclamation != null)
                _texBlueExclamation = GenerateColoredVersion(_texYellowExclamation, 0.6f, 1f);

            if (_texYellowQuestion != null)
            {
                _texBlueQuestion = GenerateColoredVersion(_texYellowQuestion, 0.6f, 1f);
                _texGreyQuestion = GenerateColoredVersion(_texYellowQuestion, 0f, 0.5f);
            }
        }

        private void OnDestroy()
        {
            var allCharacters = GameObject.FindObjectsOfType<Character>();
            foreach (var character in allCharacters)
            {
                var marker = character.transform.Find("QuestMarkerWorldUI");
                if (marker != null) Destroy(marker.gameObject);
            }
        }

        private Texture2D GenerateColoredVersion(Texture2D original, float targetHue, float valueMultiplier)
        {
            Texture2D newTex = new Texture2D(original.width, original.height, original.format, false);
            Color[] pixels = original.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                if (c.a <= 0.01f) { pixels[i] = c; continue; }
                Color.RGBToHSV(c, out float h, out float s, out float v);

                if (targetHue <= 0.01f && valueMultiplier < 0.9f) s = 0f; // gray mode
                else if (s > 0.1f) h = targetHue; // blue mode

                v *= valueMultiplier;
                pixels[i] = Color.HSVToRGB(h, s, v);
                pixels[i].a = c.a;
            }
            newTex.SetPixels(pixels);
            newTex.Apply();
            return newTex;
        }

        private void OnGUI()
        {
            if (GameData.PlayerControl == null || GameData.InCharSelect) return;
            if (_texYellowExclamation == null || _texYellowQuestion == null) LoadTextures();

            Collider[] hitColliders = Physics.OverlapSphere(GameData.PlayerControl.transform.position, QuestMarkerRadius);

            foreach (var collider in hitColliders)
            {
                
            }
        }

        private bool CheckPlayerHasItems(Quest quest)
        {
            if (quest.RequiredItems == null) return true;

            foreach (var requiredItem in quest.RequiredItems)
            {
                if (!GameData.PlayerInv.HasItem(requiredItem, false))
                {
                    if (GameData.PlayerInv.mouseSlot.MyItem != null &&
                        GameData.PlayerInv.mouseSlot.MyItem.ItemName == requiredItem.ItemName) continue;

                    bool foundInLoot = false;
                    if (GameData.TradeWindow.LootSlots != null)
                    {
                        foreach (var s in GameData.TradeWindow.LootSlots)
                        {
                            if (s.MyItem != null && s.MyItem.ItemName == requiredItem.ItemName)
                            {
                                foundInLoot = true;
                                break;
                            }
                        }
                    }
                    if (foundInLoot) continue;

                    return false;
                }
            }
            return true;
        }

        private QuestMarker.Type GetQuestAvailabilityType(Character character)
        {
            var npcDialogueManager = character.GetComponent<NPCDialogManager>();
            var questManager = character.GetComponent<QuestManager>();

            // class filter
            if (npcDialogueManager != null)
            {
                bool hasClassRestriction = npcDialogueManager.SpecificClass != null &&
                                           npcDialogueManager.SpecificClass.Count > 0;

                if (hasClassRestriction)
                {
                    // if the player class is not on the allowed list, return.
                    if (!npcDialogueManager.SpecificClass.Contains(GameData.PlayerStats.CharacterClass))
                    {
                        return QuestMarker.Type.None;
                    }
                }
            }

            bool foundBlueExclamation = false;
            bool foundBlueQuestion = false;

            // if the quest is duplicated (npcDialogueManager and QuestManager) dialogues stays
            HashSet<string> processedQuests = new HashSet<string>();

            // 1 - check dialogues (priority)
            if (npcDialogueManager != null && npcDialogueManager.MyDialogOptions != null)
            {
                foreach (var npcDialogue in npcDialogueManager.MyDialogOptions)
                {
                    if (npcDialogue == null) continue;
                    var quest = npcDialogue.QuestToAssign;
                    if (quest == null) continue;

                    processedQuests.Add(quest.DBName); // mark as processed

                    var status = CheckQuestStatus(quest);
                    if (status == QuestMarker.Type.Available) return QuestMarker.Type.Available; // yellow prevails
                    if (status == QuestMarker.Type.RepeatableNew) foundBlueExclamation = true;
                    if (status == QuestMarker.Type.RepeatableDone) foundBlueQuestion = true;
                }
            }

            // 2 - check loose quests (QuestManager)
            if (questManager != null && questManager.NPCQuests != null)
            {
                foreach (var quest in questManager.NPCQuests)
                {
                    if (quest == null) continue;

                    // ignore the quest if already passed by the dialogue, do not process twice
                    if (processedQuests.Contains(quest.DBName)) continue;

                    var status = CheckQuestStatus(quest);

                    if (status == QuestMarker.Type.Available) return QuestMarker.Type.Available; // yellow prevails
                    if (status == QuestMarker.Type.RepeatableNew) foundBlueExclamation = true;
                    if (status == QuestMarker.Type.RepeatableDone) foundBlueQuestion = true;
                }
            }

            // return the best result found
            if (foundBlueExclamation) return QuestMarker.Type.RepeatableNew;
            if (foundBlueQuestion) return QuestMarker.Type.RepeatableDone;

            return QuestMarker.Type.None;
        }

        private QuestMarker.Type CheckQuestStatus(Quest quest)
        {
            if (GameData.HasQuest.Contains(quest.DBName)) return QuestMarker.Type.None;

            if (!GameData.IsQuestDone(quest.DBName))
            {
                if (!quest.repeatable) return QuestMarker.Type.Available; // yellow
                else return QuestMarker.Type.RepeatableNew; // blue !
            }
            else if (quest.repeatable)
            {
                return QuestMarker.Type.RepeatableDone; // blue ?
            }
            return QuestMarker.Type.None;
        }

        private void AttachMarkerToCharacter(Character character, Texture2D texture, QuestMarker.Type type)
        {
            var questMarkerWorldUI = character.transform.Find("QuestMarkerWorldUI");

            if (questMarkerWorldUI != null)
            {
                var currentMarker = questMarkerWorldUI.GetComponent<QuestMarker>();
                if (currentMarker != null && currentMarker.markerType == type)
                {
                    var rawImage = questMarkerWorldUI.GetComponentInChildren<RawImage>();
                    if (rawImage != null && rawImage.texture == texture) return;
                }
                Destroy(questMarkerWorldUI.gameObject);
            }

            float npcHeight = character.GetComponent<Collider>()?.bounds.size.y ?? 2.5f;
            npcHeight = Mathf.Clamp(npcHeight, 3.0f, 3.0f);

            var marker = CreateWorldMarker(texture);
            if (marker == null) return;

            marker.name = "QuestMarkerWorldUI";
            marker.transform.SetParent(character.transform);
            marker.transform.localPosition = new Vector3(0, npcHeight - 0.05f, 0);
            marker.transform.rotation = Quaternion.identity;

            var billboard = marker.AddComponent<BillboardToCamera>();
            billboard.enabled = true;
            marker.SetActive(true);

            var questMarker = marker.AddComponent<QuestMarker>();
            questMarker.markerType = type;
        }

        private GameObject CreateWorldMarker(Texture2D texture)
        {
            GameObject canvasGO = new GameObject("QuestMarkerWorldUI");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;
            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1f, 1f);
            canvasRect.localScale = Vector3.one * 0.01f;
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(canvasGO.transform, false);
            var image = iconGO.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            var imageRect = image.GetComponent<RectTransform>();
            imageRect.sizeDelta = new Vector2(64f, 64f);
            return canvasGO;
        }

        private Texture2D LoadImageFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);

                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(buffer);
                    return tex;
                }
            }
            return null;
        }
    }

    public class BillboardToCamera : MonoBehaviour
    {
        private Camera _cam;
        private Transform _player;
        private void Start() { _cam = GameData.PlayerControl?.camera; _player = GameData.PlayerControl?.transform; }
        private void LateUpdate()
        {
            if (_cam == null) _cam = GameData.PlayerControl?.camera;
            if (_player == null) _player = GameData.PlayerControl?.transform;
            if (_cam == null || _player == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);
            if (Vector3.Distance(transform.position, _player.position) > QuestHelper.QuestMarkerRadius + 10f) Destroy(gameObject);
        }
    }

    public class QuestMarker : MonoBehaviour
    {
        public Type markerType;
        public enum Type { None, Available, TurnIn, RepeatableNew, RepeatableDone, Incomplete, Repeatable }

        // floating effect
        private Vector3 _initialPos;
        private float _bobSpeed = 2.5f;
        private float _bobHeight = 0.05f;

        private void Start()
        {
            // saves initial position from AttachMarkerToCharacter
            _initialPos = transform.localPosition;
        }
    }
}