using GorillaNetworking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Feather.Menu.Backend
{
    public class Gui : MonoBehaviour
    {
        private float categoryBarY = -50f;
        private float targetCategoryBarY = -50f;
        private float sidePanelX = -210f;
        private float targetSidePanelX = -210f;

        private float rightPanelX = Screen.width;
        private float targetRightPanelX = Screen.width;
        private string roomID = "";

        private ModCategory selectedCategory = null;
        private Vector2 scrollPosition = Vector2.zero;

        void Update()
        {
            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            float invertedY = Screen.height - mousePos.y;

            bool isHoveringTop = invertedY < 60;
            bool isHoveringSide = selectedCategory != null && mousePos.x < 220 && invertedY > 60;

            bool isHoveringRight = mousePos.x > Screen.width - 220;

            targetCategoryBarY = (isHoveringTop || isHoveringSide || isHoveringRight) ? 0f : -50f;
            targetSidePanelX = (selectedCategory != null && isHoveringSide) ? 10f : -210f;
            targetRightPanelX = isHoveringRight ? Screen.width - 210f : Screen.width;

            categoryBarY = Mathf.Lerp(categoryBarY, targetCategoryBarY, Time.deltaTime * 15f);
            sidePanelX = Mathf.Lerp(sidePanelX, targetSidePanelX, Time.deltaTime * 15f);
            rightPanelX = Mathf.Lerp(rightPanelX, targetRightPanelX, Time.deltaTime * 15f);
        }

        void OnGUI()
        {
            var rootCats = MenuBackend.categories.FindAll(c => c.Parent == null && (!c.AdminOnly || Console.ServerData.IsAdmin));
            GUI.Box(new Rect(0, categoryBarY, Screen.width, 50), "");
            GUILayout.BeginArea(new Rect(10, categoryBarY + 10, Screen.width - 20, 40));
            GUILayout.BeginHorizontal();
            foreach (var cat in rootCats)
            {
                if (GUILayout.Button(cat.Name, GUILayout.Height(30)))
                {
                    selectedCategory = cat;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (selectedCategory != null)
            {
                var subCats = selectedCategory.SubCategories.Where(s => !s.AdminOnly || Console.ServerData.IsAdmin).ToList();
                float panelHeight = Screen.height - 70;
                float contentHeight = (subCats.Count + selectedCategory.Buttons.Count) * 30;
                GUI.Box(new Rect(sidePanelX, 70, 200, panelHeight), "");
                scrollPosition = GUI.BeginScrollView(new Rect(sidePanelX, 70, 215, panelHeight), scrollPosition, new Rect(0, 0, 180, contentHeight));
                GUILayout.BeginArea(new Rect(10, 0, 180, contentHeight));
                GUILayout.BeginVertical();
                foreach (var sub in subCats)
                {
                    if (GUILayout.Button("-> " + sub.Name, GUILayout.Height(25)))
                    {
                        selectedCategory = sub;
                        scrollPosition = Vector2.zero;
                    }
                }
                foreach (var btn in selectedCategory.Buttons)
                {
                    bool isEnabled = btn.IsToggle && btn.Entry.Value;
                    Color originalColor = GUI.color;
                    if (isEnabled) GUI.color = Color.cyan;
                    if (GUILayout.Button(btn.Name, GUILayout.Height(25))) btn.Press();
                    GUI.color = originalColor;
                }
                GUILayout.EndVertical();
                GUILayout.EndArea();
                GUI.EndScrollView();
            }

            GUI.Box(new Rect(rightPanelX, 70, 200, 150), "Room Stuff");
            GUILayout.BeginArea(new Rect(rightPanelX + 10, 100, 180, 120));
            GUILayout.BeginVertical();

            roomID = GUILayout.TextField(roomID);

            if (GUILayout.Button("Join Room"))
            {
                PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(roomID, JoinType.Solo);
            }

            if (GUILayout.Button("Leave Room"))
            {
                NetworkSystem.Instance.ReturnToSinglePlayer();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}