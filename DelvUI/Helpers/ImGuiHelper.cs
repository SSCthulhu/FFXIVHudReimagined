using DelvUI.Config;
using DelvUI.Config.Tree;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DelvUI.Helpers
{
    public static class ImGuiHelper
    {
        public static void SetTooltip(string? message)
        {
            if (message == null) { return; }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(message);
            }
        }

        public static void DrawSeparator(int topSpacing, int bottomSpacing)
        {
            DrawSpacing(topSpacing);
            ImGui.Separator();
            DrawSpacing(bottomSpacing);
        }

        public static void DrawSpacing(int spacingSize)
        {
            for (int i = 0; i < spacingSize; i++)
            {
                ImGui.NewLine();
            }
        }

        public static void NewLineAndTab()
        {
            ImGui.NewLine();
            Tab();
        }

        public static void Tab()
        {
            ImGui.Text("  ");
            ImGui.SameLine();
        }

        public static Node? DrawExportResetContextMenu(Node node, bool canExport, bool canReset)
        {
            Node? nodeToReset = null;

            if (ImGui.BeginPopupContextItem("ResetContextMenu"))
            {
                if (canExport && ImGui.Selectable("Export"))
                {
                    var exportString = node.GetBase64String();
                    ImGui.SetClipboardText(exportString ?? "");
                }

                if (canReset && ImGui.Selectable("Reset"))
                {
                    ImGui.CloseCurrentPopup();
                    nodeToReset = node;
                }

                ImGui.EndPopup();
            }

            return nodeToReset;
        }

        public static (bool, bool) DrawConfirmationModal(string title, string message)
        {
            return DrawConfirmationModal(title, new string[] { message });
        }

        public static (bool, bool) DrawConfirmationModal(string title, IEnumerable<string> textLines)
        {
            ConfigurationManager.Instance.ShowingModalWindow = true;

            bool didConfirm = false;
            bool didClose = false;

            ImGui.OpenPopup(title + " ##Delvui");

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

            bool p_open = true; // i've no idea what this is used for

            if (ImGui.BeginPopupModal(title + " ##Delvui", ref p_open, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
            {
                float widestLine = 0f;
                foreach (string text in textLines)
                {
                    widestLine = Math.Max(widestLine, ImGui.CalcTextSize(text).X);
                }

                // Keep confirmation modals compact across wide screens.
                float width = Math.Clamp(widestLine + 32f, 300f, 520f);
                float maxMessageHeight = Math.Min(ImGui.GetMainViewport().Size.Y * 0.45f, 320f);
                float lineSpacing = ImGui.GetStyle().ItemSpacing.Y;
                float messageHeight = 0f;

                foreach (string text in textLines)
                {
                    // Use wrapped text size so longer lines don't get cut off.
                    messageHeight += ImGui.CalcTextSize(text, false, width - 16f).Y + lineSpacing;
                }

                float height = Math.Clamp(messageHeight + 8f, 48f, maxMessageHeight);

                ImGui.BeginChild("confirmation_modal_message", new Vector2(width, height), false);
                foreach (string text in textLines)
                {
                    ImGui.TextWrapped(text);
                }
                ImGui.EndChild();

                ImGui.NewLine();

                if (ImGui.Button("OK", new Vector2(width / 2f - 5, 24)))
                {
                    ImGui.CloseCurrentPopup();
                    didConfirm = true;
                    didClose = true;
                }

                ImGui.SetItemDefaultFocus();
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(width / 2f - 5, 24)))
                {
                    ImGui.CloseCurrentPopup();
                    didClose = true;
                }

                ImGui.EndPopup();
            }
            // close button on nav
            else
            {
                didClose = true;
            }

            if (didClose)
            {
                ConfigurationManager.Instance.ShowingModalWindow = false;
            }

            return (didConfirm, didClose);
        }

        public static bool DrawErrorModal(string message)
        {
            ConfigurationManager.Instance.ShowingModalWindow = true;

            bool didClose = false;
            ImGui.OpenPopup("Error ##Delvui");

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

            bool p_open = true; // i've no idea what this is used for
            if (ImGui.BeginPopupModal("Error ##Delvui", ref p_open, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
            {
                ImGui.Text(message);
                ImGui.NewLine();

                var textSize = ImGui.CalcTextSize(message).X;

                if (ImGui.Button("OK", new Vector2(textSize, 24)))
                {
                    ImGui.CloseCurrentPopup();
                    didClose = true;
                }

                ImGui.EndPopup();
            }
            // close button on nav
            else
            {
                didClose = true;
            }

            if (didClose)
            {
                ConfigurationManager.Instance.ShowingModalWindow = false;
            }

            return didClose;
        }

        public static (bool, bool) DrawInputModal(string title, string message, ref string value)
        {
            ConfigurationManager.Instance.ShowingModalWindow = true;

            bool didConfirm = false;
            bool didClose = false;

            ImGui.OpenPopup(title + " ##Delvui");

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

            bool p_open = true; // i've no idea what this is used for

            if (ImGui.BeginPopupModal(title + " ##Delvui", ref p_open, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
            {
                var textSize = ImGui.CalcTextSize(message).X;

                ImGui.Text(message);

                ImGui.PushItemWidth(textSize);
                ImGui.InputText("", ref value, 64);

                ImGui.NewLine();
                if (ImGui.Button("OK", new Vector2(textSize / 2f - 5, 24)))
                {
                    ImGui.CloseCurrentPopup();
                    didConfirm = true;
                    didClose = true;
                }

                ImGui.SetItemDefaultFocus();
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(textSize / 2f - 5, 24)))
                {
                    ImGui.CloseCurrentPopup();
                    didClose = true;
                }

                ImGui.EndPopup();
            }
            // close button on nav
            else
            {
                didClose = true;
            }

            if (didClose)
            {
                ConfigurationManager.Instance.ShowingModalWindow = false;
            }

            return (didConfirm, didClose);
        }

        public static string? DrawTextTagsList(string name, ref string searchText)
        {
            string? selectedTag = null;

            ImGui.SetNextWindowSize(new(200, 300));

            if (ImGui.BeginPopup(name, ImGuiWindowFlags.NoMove))
            {
                if (!ImGui.IsAnyItemActive() && !ImGui.IsAnyItemFocused() && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    ImGui.SetKeyboardFocusHere(0);
                }

                // search
                ImGui.InputText("", ref searchText, 64);

                List<string> keys = new List<string>();
                keys.AddRange(TextTagsHelper.TextTags.Keys);
                keys.AddRange(TextTagsHelper.ExpTags.Keys);
                keys.AddRange(TextTagsHelper.CharaTextTags.Keys);

                foreach (string key in keys)
                {
                    if (searchText.Length > 0 && !key.Contains(searchText))
                    {
                        continue;
                    }

                    // tag
                    if (ImGui.Selectable(key))
                    {
                        selectedTag = key;
                        searchText = "";
                    }

                    // help tooltip
                    if (ImGui.IsItemHovered() && Plugin.ObjectTable.LocalPlayer != null)
                    {
                        string formattedText = TextTagsHelper.FormattedText(key, Plugin.ObjectTable.LocalPlayer);

                        if (formattedText.Length > 0)
                        {
                            ImGui.SetTooltip("Example: " + formattedText);
                        }
                    }
                }

                ImGui.EndPopup();
            }

            return selectedTag;
        }
    }
}
