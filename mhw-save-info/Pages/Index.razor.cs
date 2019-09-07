using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazor.FileReader;
using MHWSaveUtils;
using Microsoft.AspNetCore.Components;

namespace mhw_save_info.Pages
{
    public class IndexBase : ComponentBase
    {
        [Inject]
        private IFileReaderService fileReaderService { get; set; }

        protected ElementReference dropTargetElement;
        protected IFileReaderRef dropReference;
        protected List<string> dropClasses = new List<string>() { dropTargetClass };

        protected const string dropTargetDragClass = "droptarget-drag";
        protected const string dropTargetClass = "droptarget";

        public string DropClass
        {
            get
            {
                return string.Join(" ", dropClasses);
            }
        }

        protected string Output { get; set; }
        protected List<IFileInfo> FileList { get; } = new List<IFileInfo>();

        protected override async Task OnAfterRenderAsync(bool isFirstRender)
        {
            dropReference = fileReaderService.CreateReference(dropTargetElement);
            await dropReference.RegisterDropEventsAsync();
        }

        public async Task ClearFile()
        {
            await dropReference.ClearValue();
            await RefreshFileList();
        }

        protected void OnDragEnter(EventArgs _)
        {
            dropClasses.Add(dropTargetDragClass);
        }

        protected void OnDragLeave(EventArgs _)
        {
            dropClasses.Remove(dropTargetDragClass);
        }

        protected async Task OnDrop(EventArgs _)
        {
            Output += "Dropped a file.";
            dropClasses.Remove(dropTargetDragClass);
            StateHasChanged();
            await RefreshFileList();
            await ReadFile();
        }

        private async Task RefreshFileList()
        {
            FileList.Clear();

            foreach (IFileReference file in await dropReference.EnumerateFilesAsync())
            {
                IFileInfo fileInfo = await file.ReadFileInfoAsync();
                FileList.Add(fileInfo);
            }
            
            StateHasChanged();
        }

        private static readonly string NewLine = Environment.NewLine;

        private void Log(string str, bool update = true)
        {
            Output += $"[{DateTime.Now.ToString("HH:mm:sss.fff")}] {str}{NewLine}";
            if (update)
                StateHasChanged();
        }

        public async Task ReadFile()
        {
            Output = string.Empty;
            StateHasChanged();

            foreach (IFileReference file in await dropReference.EnumerateFilesAsync())
            {
                var stream = new MemoryStream();

                Log($"Decrypting save data, this can be long...");

                Stopwatch sw = Stopwatch.StartNew();

                using (Stream inputStream = await file.CreateMemoryStreamAsync())
                {
                    await Crypto.ParallelDecryptAsync(inputStream, stream, CancellationToken.None);
                }

                sw.Stop();

                Log($"Decryption done. ({sw.ElapsedMilliseconds} ms)");
                Output += NewLine;

                using (var weaponUsageReader = new WeaponUsageReader(stream))
                {
                    IEnumerable<WeaponUsageSaveSlotInfo> slots = weaponUsageReader.Read();

                    foreach (WeaponUsageSaveSlotInfo slotInfo in slots)
                    {
                        string playtime = MiscUtils.PlaytimeToGameString(slotInfo.Playtime);
                        Log($"Name: {slotInfo.Name}, Rank: {slotInfo.Rank}, Play time: {playtime}", false);
                    }
                }

                StateHasChanged();
            }
        }
    }
}
