using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

namespace CyberSecurityChatBot
{
    public class ChatBot
    {
        private KeywordResponder _keywords;
        private SentimentDetector _sentiment;
        private MemoryStore _memory;
        private QuestionAnswerer _questionAnswerer;
        private bool _awaitingName = true;
        private string _lastTopic = "";
        // Venting / emotional support state
        private bool _inVentingMode = false;
        private List<string> _ventHistory = new List<string>();

        // Expose venting state to the UI
        public bool IsInVentingMode { get { return _inVentingMode; } }

        public ChatBot()
        {
            _keywords = new KeywordResponder();
            _sentiment = new SentimentDetector();
            _memory = new MemoryStore();
            _questionAnswerer = new QuestionAnswerer();
        }

        // Expose internal memory for UI/manager integration (read-only)
        public MemoryStore Memory => _memory;

        public string GetGreeting()
        {
            return "Hello there! What is your name?";
        }

        public string ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "No worries. Please type a message or ask a question.";
            }

            input = input.Trim();
            string lowerInput = input.ToLower();

            // Step 1: If awaiting name, capture it
            if (_awaitingName)
            {
                if (!string.IsNullOrWhiteSpace(input))
                {
                    _memory.UserName = CapitalizeName(input);
                    _awaitingName = false;
                    return $"Welcome {_memory.UserName}! I am KANYA SHIELD, your Cybersecurity Awareness Assistant. I'm here to help you stay safe online and learn about cybersecurity. What would you like to know?";
                }
                else
                {
                    return "Oops, I didn't catch your name. Please type your name so we can continue:";
                }
            }

            // Step 1.5: Handle explicit joke requests (PRIORITY)
            if (lowerInput.Contains("tell me a joke") || lowerInput == "joke" || lowerInput.Contains("make me laugh") || lowerInput.Contains("tell joke"))
            {
                return GetRandomJoke();
            }

            // Step 2: Handle follow-up phrases
            if (lowerInput.Contains("tell me more") || lowerInput.Contains("explain more") || lowerInput.Contains("more"))
            {
                if (!string.IsNullOrWhiteSpace(_lastTopic))
                {
                    string moreResponse = _keywords.GetResponse(_lastTopic);
                    if (!string.IsNullOrWhiteSpace(moreResponse))
                    {
                        return moreResponse;
                    }
                }
            }

            // Step 3: Detect sentiment
            Sentiment detectedSentiment = _sentiment.Detect(input);
            string sentimentOpener = _sentiment.GetSentimentResponse(detectedSentiment);

            // Step 3.2: Handle venting requests (NEW!)
            if (!_inVentingMode && (lowerInput.Contains("vent") || lowerInput.Contains("frustrated") || lowerInput.Contains("angry") || 
                lowerInput.Contains("upset") || lowerInput.Contains("stressed") || lowerInput.Contains("worried about") ||
                lowerInput.Contains("i hate") || (lowerInput.Contains("this is") && lowerInput.Contains("annoying"))))
            {
                _inVentingMode = true;
                _ventHistory.Clear();
                string ventingResponse = GetVentingSupport(input, detectedSentiment);
                return ventingResponse + " If you'd like to keep talking about how you feel, just go on — I'm listening. Say 'stop venting' when you want practical help.";
            }

            // If already in venting mode, capture emotions and respond empathetically
            if (_inVentingMode)
            {
                _ventHistory.Add(input);
                string ongoingResponse = GetVentingSupport(input, detectedSentiment);
                return ongoingResponse + " (You can say 'stop venting' to switch to technical help or ask 'what can I do' for practical steps.)";
            }

            // Step 3.5: Check for detailed question answers BEFORE keyword matching
            string detailedAnswer = _questionAnswerer.GetAnswer(input);
            if (!string.IsNullOrWhiteSpace(detailedAnswer))
            {
                _memory.ExtractFavouriteTopic(input);
                _lastTopic = input;
                return sentimentOpener + detailedAnswer;
            }

            // Step 4: Run keyword detection
            string keywordResponse = _keywords.GetResponse(input);
            if (!string.IsNullOrWhiteSpace(keywordResponse))
            {
                _memory.ExtractFavouriteTopic(input);
                _lastTopic = input;
                return sentimentOpener + keywordResponse;
            }

            // Step 5: Handle special phrases
            if (lowerInput == "how are you")
            {
                return "I'm doing great and ready to help you stay safe online!";
            }
            if (lowerInput.Contains("stop venting") || lowerInput.Contains("i'm ready for help") || lowerInput.Contains("i want help") || lowerInput.Contains("what can i do"))
            {
                // User wants to exit venting/emotional support mode
                if (_inVentingMode)
                {
                    _inVentingMode = false;
                    string summary = SummarizeVentHistory();
                    return "Thank you for sharing — I hear you. " + summary + " If you want practical steps, ask me about passwords, 2FA, reporting scams, or say 'give me steps'.";
                }
            }

            if (lowerInput.Contains("what can you do") || lowerInput.Contains("what can i ask"))
            {
                var keywords = _keywords.GetAllKeywords();
                return "I can help you with questions about: " + string.Join(", ", keywords) + " and more! I also answer detailed questions about cybersecurity practices and threats.";
            }
            if (lowerInput.Contains("purpose"))
            {
                return "My purpose is to teach cybersecurity and help protect users from online threats. I provide detailed explanations and practical advice on staying safe online.";
            }

            // General conversational fallback (chat like ChatGPT/Copilot)
            string generalChat = GetGeneralChatResponse(input, detectedSentiment);
            if (!string.IsNullOrWhiteSpace(generalChat))
            {
                return generalChat;
            }

            // Step 6: Fallback - provide specific clarifying questions to help user
            // Instead of generic "I don't know" responses, ask what they want to learn about
            string[] clarifyingResponses = new string[]
            {
                "I'd like to help! Could you ask about one of these topics: phishing, passwords, malware, ransomware, two-factor authentication, privacy, VPNs, firewalls, social engineering, or scams?",
                "I specialize in cybersecurity topics. Would you like to know about: passwords, phishing, malware, privacy, 2FA, VPNs, firewalls, or how to report a scam?",
                "Let me help you with cybersecurity! I can answer questions about phishing, passwords, malware, privacy, VPNs, firewalls, ransomware, or security best practices. What interests you?",
                "I can assist with cybersecurity topics like: passwords, phishing, malware, two-factor authentication, VPNs, privacy, firewalls, social engineering, and scam reporting. Which would you like to explore?",
                "I'm here to help with cybersecurity! Ask me about passwords, phishing, malware, privacy, VPNs, firewalls, ransomware, identity theft, or how to stay safe online."
            };

            Random random = new Random();
            return clarifyingResponses[random.Next(clarifyingResponses.Length)];
        }

        // ================= NAME =================
        private string CapitalizeName(string name)
        {
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(name.Trim().ToLower());
        }

        // ================= VENTING SUPPORT (NEW!) =================
        private string GetVentingSupport(string input, Sentiment sentiment)
        {
            // Basic empathetic replies adjusted by detected sentiment
            List<string> supportive = new List<string>();

            if (sentiment == Sentiment.Frustrated || sentiment == Sentiment.Worried)
            {
                supportive.Add($"I hear you, {_memory.UserName}. That sounds really tough. Thank you for sharing that with me.");
                supportive.Add("It's understandable to feel overwhelmed. You're not alone in this.");
                supportive.Add("Taking a deep breath can help a bit — try breathing in for 4 seconds, hold 4, breathe out 6.");
                supportive.Add("Would you like some grounding techniques or practical steps to regain control?");
            }
            else if (sentiment == Sentiment.Neutral)
            {
                supportive.Add($"I'm listening, {_memory.UserName}. Tell me more if you'd like to keep talking.");
                supportive.Add("Sometimes talking through the details helps clarify next steps.");
                supportive.Add("Would you like to try a short breathing exercise or get practical cybersecurity steps?");
            }
            else // Positive or unknown (Happy)
            {
                supportive.Add($"I appreciate you sharing that, {_memory.UserName}. I'm here to support you.");
                supportive.Add("If you want, I can help with practical advice or we can just keep talking about how you feel.");
                supportive.Add("Would you like steps to protect your accounts right now?");
            }

            // If user asks for specific emotional help keywords, offer resources
            string lower = input.ToLower();
            if (lower.Contains("panic") || lower.Contains("anxiety") || lower.Contains("can't breathe") || lower.Contains("help me"))
            {
                supportive.Add("If you're feeling panicky or unsafe, please consider contacting local emergency services or a trusted person. If it's about privacy or fraud, I can show reporting resources.");
            }

            // If user requests breathing/grounding explicitly
            if (lower.Contains("breath") || lower.Contains("breathing") || lower.Contains("ground") || lower.Contains("grounding"))
            {
                supportive.Add(GetBreathingExercise());
                supportive.Add(GetGroundingExercise());
            }

            // Choose a combined response
            Random rand = new Random();
            string chosen = supportive[rand.Next(supportive.Count)];

            // Add gentle prompts and track in vent history
            _ventHistory.Add(input);

            string nextPrompts = " You can keep venting, ask for breathing or grounding techniques, or say 'stop venting' to switch to practical steps.";

            return chosen + nextPrompts;
        }

        private string SummarizeVentHistory()
        {
            if (_ventHistory.Count == 0)
                return "You shared some concerns.";

            // Summarize last two entries
            var last = _ventHistory.TakeLast(2).ToList();
            return $"I heard you describe: '{string.Join("; ", last)}'.";
        }

        // ================= COPING EXERCISES =================
        private string GetBreathingExercise()
        {
            return "Breathing exercise: Sit comfortably. Breathe in slowly for 4 seconds, hold for 4, breathe out for 6. Repeat 4 times. Notice how your body feels.";
        }

        private string GetGroundingExercise()
        {
            return "Grounding exercise (5-4-3-2-1): Name 5 things you can see, 4 you can touch, 3 you can hear, 2 you can smell, 1 you can taste. Use this to anchor yourself.";
        }

        private string GetCopingTips()
        {
            return "Coping tips: 1) Take short breaks and breathe. 2) Prioritize small tasks. 3) Reach out to someone you trust. 4) Use a password manager and enable 2FA to reduce worry about accounts.";
        }

        // ================= GENERAL CHAT =================
        // Constrained small-talk: only allow brief greetings, light humour, or enter venting mode.
        // Do NOT attempt to answer technical questions or invent facts here — those must come from
        // QuestionAnswerer or KeywordResponder.
        private string GetGeneralChatResponse(string input, Sentiment sentiment)
        {
            string lower = input.ToLower();


            if (lower.Contains("how are you") || lower.Contains("how's it going") || lower.Contains("what's up"))
            {
                return "I'm a virtual assistant — ready to help with cybersecurity questions or listen if you'd like to vent.";
            }

            // If user mentions stress/challenges explicitly outside venting mode, enter venting mode
            if (lower.Contains("stress") || lower.Contains("challenge") || lower.Contains("overwhelmed") || lower.Contains("struggling"))
            {
                _inVentingMode = true;
                _ventHistory.Clear();
                return "It sounds like you're dealing with some stress. I'm here to listen — tell me what's been going on, or say 'breathing' for a short exercise.";
            }

            // Reflective acknowledgements based on sentiment (non-technical)
            if (sentiment == Sentiment.Happy)
            {
                return "That's nice to hear — tell me more or ask a cybersecurity question.";
            }
            else if (sentiment == Sentiment.Curious)
            {
                return "I can help with facts from my cybersecurity topics — what would you like to learn about?";
            }
            else if (sentiment == Sentiment.Frustrated || sentiment == Sentiment.Worried)
            {
                return "I hear concern — I can listen or provide practical steps for security issues. Which would you prefer?";
            }

            // For anything else, return null to let the main flow give a grounded fallback
            return null;
        }

        // ================= JOKE BANK =================
        private string GetRandomJoke()
        {
            string[] jokes = new string[]
            {
                "Why did the computer show up at work late? It had a hard drive!",
                "Why do cybersecurity experts never get lonely? Because they're always making connections!",
                "Why did the password go to the doctor? Because it was feeling weak!",
                "How many cybersecurity professionals does it take to change a lightbulb? Three: one to change it and two to discuss how the old one could have been exploited.",
                "Why don't phishers ever play hide and seek? Because good malware is always seeking!",
                "I tried to make a joke about UDP, but I wasn't sure if you'd get it.",
                "Why did the hacker go to school? To improve their malware skills!",
                "What's a cybersecurity expert's favorite exercise? Running firewalls!",
                "Did you hear about the database that went on vacation? It needed to recharge its cache!",
                "Why do programmers always mix up Halloween and Christmas? Because Oct 31 = Dec 25!"
            };

            Random random = new Random();
            return jokes[random.Next(jokes.Length)];
        }

        // ================= START METHOD =================
        public void Start()
        {
            // Legacy console-based method. Use ProcessInput() in WPF instead.
            Console.WriteLine(GetGreeting());
        }
    }
}

