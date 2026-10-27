using BeforeItWasYou.Core.Narrative;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeforeItWasYou.UI
{
    /// <summary>
    /// Pantalla del móvil: chat con burbujas, hora, indicador de escritura y respuestas elegibles (1, 2, 3).
    /// Solo dibuja; la lógica de la conversación vive en PhoneLog y en el director narrativo.
    /// </summary>
    public sealed class PhoneView
    {
        private readonly VisualElement _phone;
        private readonly ScrollView _messages;
        private readonly VisualElement _replies;
        private readonly Label _clock;
        private readonly Label _presence;
        private readonly Label _composer;
        private VisualElement _typing;
        private PhoneLog _log;

        public PhoneView(VisualElement root)
        {
            _phone = root.Q("phone");
            _messages = root.Q<ScrollView>("phone-messages");
            _replies = root.Q("phone-replies");
            _clock = root.Q<Label>("phone-clock");
            _presence = root.Q<Label>("phone-presence");
            _composer = root.Q<Label>("phone-composer");
        }

        public bool IsOpen { get; private set; }

        public void Bind(PhoneLog log)
        {
            if (_log != null) _log.Changed -= Rebuild;
            _log = log;
            _log.Changed += Rebuild;
            _phone.Q<Label>("phone-contact").text = log.Contact;
            Rebuild();
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            _phone.EnableInClassList("phone--away", !open);
            if (open)
            {
                _log?.MarkAllRead();
                ScrollToEnd();
            }
        }

        public void SetClock(string time) => _clock.text = time;

        private bool _callHint;

        /// <summary>Muestra en el móvil la opción de llamar a emergencias (tecla 3).</summary>
        public void SetCallHint(bool visible)
        {
            if (_callHint == visible) return;
            _callHint = visible;
            Rebuild();
        }

        public void SetTyping(bool typing)
        {
            _presence.text = typing ? "escribiendo..." : "en línea";
            if (typing && _typing == null)
            {
                _typing = new VisualElement();
                _typing.AddToClassList("typing");
                for (var i = 0; i < 3; i++)
                {
                    var dot = new VisualElement();
                    dot.AddToClassList("typing__dot");
                    _typing.Add(dot);
                }
                _messages.Add(_typing);
                _typing.schedule.Execute(Animate).Every(140);
                ScrollToEnd();
            }
            else if (!typing && _typing != null)
            {
                _typing.RemoveFromHierarchy();
                _typing = null;
            }
        }

        private void Animate(TimerState state)
        {
            if (_typing == null) return;
            var t = state.now / 1000f;
            var i = 0;
            foreach (var dot in _typing.Children())
            {
                dot.style.opacity = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 4f - i * 0.7f));
                i++;
            }
        }

        private void Rebuild()
        {
            if (_log == null) return;
            var typing = _typing != null;
            _messages.Clear();
            _typing = null;
            foreach (var m in _log.Messages)
            {
                var bubble = new VisualElement();
                bubble.AddToClassList("bubble");
                bubble.AddToClassList(m.Sender == Sender.Player ? "bubble--out" : "bubble--in");
                var text = new Label(m.Text);
                text.AddToClassList("bubble__text");
                var time = new Label(m.Sender == Sender.Player ? m.Time + "  ✓✓" : m.Time);
                time.AddToClassList("bubble__time");
                bubble.Add(text);
                bubble.Add(time);
                _messages.Add(bubble);
            }
            if (typing) SetTyping(true);

            _replies.Clear();
            for (var i = 0; i < _log.Replies.Count; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("reply");
                var key = new Label((i + 1).ToString());
                key.AddToClassList("reply__key");
                var text = new Label(_log.Replies[i].Text);
                text.AddToClassList("reply__text");
                row.Add(key);
                row.Add(text);
                _replies.Add(row);
            }
            if (_callHint)
            {
                var call = new VisualElement();
                call.AddToClassList("reply");
                call.AddToClassList("reply--call");
                var key = new Label("3");
                key.AddToClassList("reply__key");
                var text = new Label("Llamar a emergencias");
                text.AddToClassList("reply__text");
                call.Add(key);
                call.Add(text);
                _replies.Add(call);
            }
            _composer.text = _log.Replies.Count > 0 ? "Elige qué responder" : "Mensaje";
            ScrollToEnd();
        }

        private void ScrollToEnd()
        {
            _messages.schedule.Execute(() =>
            {
                var last = _messages.contentContainer.childCount > 0 ? _messages.contentContainer[_messages.contentContainer.childCount - 1] : null;
                if (last != null) _messages.ScrollTo(last);
            }).StartingIn(30);
        }
    }
}
