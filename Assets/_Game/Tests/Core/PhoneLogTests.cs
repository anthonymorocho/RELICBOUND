using BeforeItWasYou.Core.Narrative;
using NUnit.Framework;

namespace BeforeItWasYou.Core.Tests
{
    public sealed class PhoneLogTests
    {
        [Test]
        public void MensajesSinLeer_RespuestaYLectura()
        {
            var log = new PhoneLog("Lucía");
            var received = 0;
            log.Received += _ => received++;
            log.Add(new PhoneMessage("lucia_01", Sender.Contact, "¿Ya llegaste?", "21:12"));
            log.Add(new PhoneMessage("lucia_01", Sender.Contact, "¿Ya llegaste?", "21:12"));
            Assert.AreEqual(1, log.Messages.Count, "Un mismo mensaje no se duplica tras cargar.");
            Assert.AreEqual(1, log.Unread);
            log.OfferReplies(new PhoneReply("daniel_01", "Sí. Voy entrando."));
            string sent = null;
            log.ReplySent += r => sent = r.Id;
            Assert.IsTrue(log.Choose(0, "21:13"));
            Assert.AreEqual("daniel_01", sent);
            Assert.AreEqual(0, log.Replies.Count);
            log.MarkAllRead();
            Assert.AreEqual(0, log.Unread);
            Assert.AreEqual(1, received);
        }
    }
}
