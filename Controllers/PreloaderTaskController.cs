﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;
using NoblegardenLauncherSharp.Models;
using NoblegardenLauncherSharp.Structures;

namespace NoblegardenLauncherSharp.Controllers
{
    public class PreloaderTaskController
    {
        private readonly ElementSearcherController ElementSearcher;
        private readonly SiteAPIModel SiteAPI = SiteAPIModel.GetInstance();
        private TextBlock CurrentLoadingStepText;
        private UpdateServerAPIModel UpdateServerAPI;
        public PreloaderTaskController () {
            ElementSearcher = ElementSearcherController.GetInstance();
            GetCurrentLoadingStepView();
        }

        public async Task GetUpdateServerAddress() {
            CurrentLoadingStepText.Text = Globals.LOADING_TEXTS[(int)Globals.LOADING_STEPS.GET_SERVER_ADDRESS];
            var updateServerAdressResponse = await SiteAPI.GetUpdateServerAddress();
            string updateServerIP = (string)updateServerAdressResponse.GetFormattedData();
            UpdateServerAPI = UpdateServerAPIModel.Init($"http://{updateServerIP}");
        }

        public async Task CheckLauncherVersion() {
            CurrentLoadingStepText.Text = Globals.LOADING_TEXTS[(int)Globals.LOADING_STEPS.CHECK_LAUNCHER_VERSION];
            var launcherVersionResponse = await UpdateServerAPI.GetActualLauncherVersion();
            string actualLauncherVersion = (string)launcherVersionResponse.GetFormattedData().version;
            if (actualLauncherVersion == "") {
                throw new Exception("Сервер не вернул актуальной версии лаунчера");
            }
            if (actualLauncherVersion != Globals.LAUNCHER_VERSION) {
                throw new Exception("Используется неактуальная версия лаунчера");
            }
        }

        public async Task DrawVisualInformation() {
            CurrentLoadingStepText.Text = Globals.LOADING_TEXTS[(int)Globals.LOADING_STEPS.GET_LAST_UPDATES];
            await Task.WhenAll(
                Task.Run(() => DrawCurrentOnline()),
                Task.Run(() => DrawLastNews()),
                Task.Run(() => SetDiscordLink()),
                Task.Run(() => SetVKLink()),
                Task.Run(() => GetAndDrawCustomPatches()),
                Task.Run(() => GetBasePatches())
            );
        }

        public void PlaySuccessLoadAnimation() {
            CurrentLoadingStepText.Text = "";
            var fadeOutAnim = ElementSearcher.FindStoryboard("FadeOutModalBG");
            if (fadeOutAnim != null) {
                fadeOutAnim.Begin();
            }
        }

        private async Task DrawCurrentOnline() {
            var onlineResponse = await SiteAPI.GetOnlineCount();
            var wordForms = new string[] { "игрок", "игрока", "игроков" };
            string text = "Не найдено";
            if (onlineResponse.IsOK) {
                try {
                    int playersCount = Int32.Parse(onlineResponse.GetFormattedData());
                    string wordForm = Globals.GetRightWordForm(playersCount, wordForms);
                    text = $"{playersCount} {wordForm}";
                }
                catch {
                    text = onlineResponse.GetFormattedData();
                }
            }

            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var onlineCountBlock = (TextBlock)ElementSearcher.FindName("CurrentOnline");
                    onlineCountBlock.Text = text;
                })
            );
        }
        private async Task DrawLastNews() {
            var newsResponse = await SiteAPI.GetLastNews();
            var newsAsJSONArray = newsResponse.GetFormattedData();

            var newsList = new List<SitePost>();
            for (int i = 0; i < newsAsJSONArray.Count; i++) {
                var current = newsAsJSONArray[i];
                newsList.Add(
                    new SitePost(
                        (string)current.title,
                        (string)current.preview,
                        (string)current.link,
                        (string)current.author,
                        (string)current.authorLink
                   )
                );
            }

            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var newsListView = (ListView)ElementSearcher.FindName("LastNewsView");
                    newsListView.ItemsSource = newsList;
                })
            );
        }

        private async Task SetDiscordLink() {
            var discordLinkResponse = await SiteAPI.GetActualDiscordLink();
            var link = discordLinkResponse.GetFormattedData();

            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var discordView = (Rectangle)ElementSearcher.FindName("DiscordView");
                    discordView.Tag = link;
                })
            );
        }
        private async Task SetVKLink() {
            var discordLinkResponse = await SiteAPI.GetActualVKLink();
            var link = discordLinkResponse.GetFormattedData();

            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var vkView = (Rectangle)ElementSearcher.FindName("VKView");
                    vkView.Tag = link;
                })
            );
        }

        private async Task GetBasePatches() {
            var defaultPatchesResponse = await UpdateServerAPI.GetBasePatches();
            var patchesInfo = defaultPatchesResponse.GetFormattedData();
            var defaultPatches = JObjectConverter.ConvertToNecessaryPatchesList(patchesInfo);
            Globals.Patches = new NoblePatchGroupModel<NecessaryPatchModel>(defaultPatches);
        }

        private async Task GetAndDrawCustomPatches() {
            var customPatchesResponse = await UpdateServerAPI.GetCustomPatches();
            var patchesInfo = customPatchesResponse.GetFormattedData();
            List<CustomPatchModel> customPatches = JObjectConverter.ConvertToCustomPatchesList(patchesInfo);
            Globals.CustomPatches = new NoblePatchGroupModel<CustomPatchModel>(customPatches);

            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var customPatchesView = (ListView)ElementSearcher.FindName("CustomPatchesView");
                    customPatchesView.ItemsSource = Globals.CustomPatches.List;
                })
            );
        } 
        private TextBlock GetCurrentLoadingStepView() {
            if (CurrentLoadingStepText != null)
                return CurrentLoadingStepText;

            CurrentLoadingStepText = (TextBlock)ElementSearcher.FindName("CurrentLoadingStep");
            return CurrentLoadingStepText;
        }
    }
}
