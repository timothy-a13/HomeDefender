using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorApp1.Components
{
    public partial class HlsPlayer : IAsyncDisposable
    {
        [Inject] IJSRuntime? JS { get; set; }
        private DotNetObjectReference<HlsPlayer>? _instance { get; set; }
        protected ElementReference element { get; set; }
        private bool init;
        private string? info;

        private string Id { get; set; } = Guid.NewGuid().ToString();

        /// 資源類型
        [Parameter]
        public string? SrcType { get; set; } = "application/x-mpegURL";

        /// 資源地址
        [Parameter]
        public string? SrcUrl { get; set; }

        [Parameter]
        public int Width { get; set; } = 300;

        [Parameter]
        public int Height { get; set; } = 200;

        [Parameter]
        public bool Controls { get; set; } = false;

        [Parameter]
        public bool Autoplay { get; set; } = false;

        [Parameter]
        public bool Muted { get; set; } = false;

        [Parameter]
        public string Preload { get; set; } = "auto";

        [Parameter]
        public string? Class { get; set; }

        [Parameter]
        public PlayerOption? Option { get; set; }

        [Parameter]
        public bool Debug { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender)
                {
                    _instance = DotNetObjectReference.Create(this);

                    Option ??= new PlayerOption()
                    {
                        Width = Width,
                        Height = Height,
                        Controls = Controls,
                        Autoplay = Autoplay,
                        Preload = Preload,
                        Muted = Muted,
                        SrcUrl = SrcUrl,
                        SrcType = SrcType,
                        Class = Class,
                    };

                    try
                    {
                        await JS!.InvokeVoidAsync("load_hls", _instance, "video_" + Id, Option);
                    }
                    catch (Exception e)
                    {
                        info = e.Message;
                        if (Debug) StateHasChanged();
                        Console.WriteLine(info);
                        if (OnError is not null)
                            await OnError.Invoke(info);
                    }
                }
            }
            catch (Exception e)
            {
                if (OnError is not null)
                    await OnError.Invoke(e.Message);
            }
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (JS is not null)
            {
                //await JS.InvokeVoidAsync("destroy", Id);
                //await JS.DisposeAsync();
            }
        }

        [Parameter]
        public Func<string, Task>? OnError { get; set; }

        [JSInvokable]
        public void GetInit(bool init) => this.init = init;

        [JSInvokable]
        public async Task GetError(string error)
        {
            info = error;
            if (Debug) StateHasChanged();
            if (OnError is not null) await OnError.Invoke(error);
        }
    }
}
