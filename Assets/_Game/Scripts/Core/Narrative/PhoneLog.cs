using System;
using System.Collections.Generic;

namespace BeforeItWasYou.Core.Narrative
{
    public enum Sender
    {
        Contact,
        Player,
    }

    public sealed class PhoneMessage
    {
        public PhoneMessage(string id, Sender sender, string text, string time)
        {
            Id = id;
            Sender = sender;
            Text = text;
            Time = time;
        }

        public string Id { get; }
        public Sender Sender { get; }
        public string Text { get; }
        public string Time { get; }
    }

    /// <summary>Respuesta que Daniel puede escribir. Al elegirla se envía y se dispara la continuación.</summary>
    public sealed class PhoneReply
    {
        public PhoneReply(string id, string text)
        {
            Id = id;
            Text = text;
        }

        public string Id { get; }
        public string Text { get; }
    }

    /// <summary>
    /// Conversación de Daniel con Lucía: historial, mensajes sin leer y respuestas disponibles.
    /// El estado (ids recibidos, leídos y enviados) se guarda en el snapshot; los textos viven en el guion.
    /// </summary>
    public sealed class PhoneLog
    {
        private readonly List<PhoneMessage> _messages = new List<PhoneMessage>();
        private readonly List<PhoneReply> _replies = new List<PhoneReply>();
        private readonly HashSet<string> _read = new HashSet<string>();

        public string Contact { get; }

        public PhoneLog(string contact)
        {
            Contact = contact;
        }

        public IReadOnlyList<PhoneMessage> Messages => _messages;
        public IReadOnlyList<PhoneReply> Replies => _replies;

        public event Action<PhoneMessage> Received;
        public event Action<PhoneReply> ReplySent;
        public event Action Changed;

        public int Unread
        {
            get
            {
                var count = 0;
                foreach (var m in _messages)
                {
                    if (m.Sender == Sender.Contact && !_read.Contains(m.Id)) count++;
                }
                return count;
            }
        }

        public bool Has(string id) => _messages.Exists(m => m.Id == id);

        /// <summary>Añade un mensaje (idempotente por id: tras cargar partida no se duplica).</summary>
        public void Add(PhoneMessage message, bool silent = false)
        {
            if (Has(message.Id)) return;
            _messages.Add(message);
            if (message.Sender == Sender.Player) _read.Add(message.Id);
            if (!silent && message.Sender == Sender.Contact) Received?.Invoke(message);
            Changed?.Invoke();
        }

        public void OfferReplies(params PhoneReply[] replies)
        {
            _replies.Clear();
            _replies.AddRange(replies);
            Changed?.Invoke();
        }

        public void ClearReplies()
        {
            if (_replies.Count == 0) return;
            _replies.Clear();
            Changed?.Invoke();
        }

        public bool Choose(int index, string time)
        {
            if (index < 0 || index >= _replies.Count) return false;
            var reply = _replies[index];
            _replies.Clear();
            Add(new PhoneMessage(reply.Id, Sender.Player, reply.Text, time));
            ReplySent?.Invoke(reply);
            return true;
        }

        public void MarkAllRead()
        {
            var changed = false;
            foreach (var m in _messages) changed |= _read.Add(m.Id);
            if (changed) Changed?.Invoke();
        }

        public IEnumerable<string> ReadIds => _read;

        public void RestoreRead(IEnumerable<string> ids)
        {
            foreach (var id in ids) _read.Add(id);
            Changed?.Invoke();
        }
    }
}
