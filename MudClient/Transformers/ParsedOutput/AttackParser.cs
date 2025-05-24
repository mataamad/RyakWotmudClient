using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MudClient {
    internal class AttackParser {

        // todo: this doesn't handle colours
        // 1 \x1B[32mYou lance an oozing ball of mud's right arm into bloody fragments!
        // same for people attacking me but the start of the regex happens to capture it

        static string meleeAttack = "blast|cleave|crush|hack|hit|lance|pierce|pound|scythe|shoot|slash|slice|smite|stab|sting|strike|whip";
        static string meleeAttacked = "blasts|cleaves|crushes|hacks|hits|lances|pierces|pounds|scythes|shoots|slashes|slices|smites|stabs|stings|strikes|whips";
        static string meleeAttacking = "blasting|cleaving|crushing|hacking|hitting|lancing|piercing|pounding|scything|shooting|slashing|slicing|smiting|stabbing|stinging|striking|whipping";
        static string meleeBodypart = "body|branch|crown|dorsal fin|head|left arm|left fin|left foot|left foreleg|left hand|left leg|left paw|left wing|right arm|right fin|right foot|right foreleg|right hand|right leg|right paw|right wing|roots|tail fin|trunk";
        static string meleeDamage = "into bloody fragments|extremely hard|very hard|hard"; // todo map to damage ranges
        static string meleeTickle = "barely|tickle|barely tickle";
        static string meleeTickles = "barely|tickles|barely tickles";  // todo map to damage ranges

        static string meleeFlowList = "assume|flow effortlessly into|gracefully flow into|spin effortlessly into";
        static string meleeSwordForms = "Arc of the Moon|Bundling Straw|Cat on Hot Sand|Hummingbird Kisses the Honeyrose|Lightning of Three Prongs|Lizard in the Thornbush|Low Wind Rising|Moon Rises Over the Water|Parting the Silk|Ribbon in the Air|Stones Falling from the Cliff|Striking the Spark|The Boar Rushes Down the Mountain|The Cat Dances on the Wall|The Courtier Taps His Fan|The Falcon Swoops|The Falling Leaf|The Heron Spreads Its Wings|The Kingfisher Takes a Silverback|The River Undercuts the Bank|The Swallow Takes Flight|The Wood Grouse Dances|Thistledown Floats on the Wind|Tower of Morning|Water Flows Downhill|Whirlwind on the Mountain|Wind and Rain";

        Regex youTickle = new($@"^(\\x1B\[0m)?\\x1B\[32mYou ({meleeTickle}) (.*)'s ({meleeBodypart}) with your ({meleeAttack})\.$", RegexOptions.Compiled);
        Regex youBarely = new($@"^(\\x1B\[0m)?\\x1B\[32mYou barely ({meleeAttack}) (.*)'s ({meleeBodypart})\.$", RegexOptions.Compiled);
        Regex youAttack = new($@"^(\\x1B\[0m)?\\x1B\[32mYou ({meleeAttack}) (.*)'s ({meleeBodypart})(\.|!)$", RegexOptions.Compiled);
        Regex youAttackInto = new($@"^(\\x1B\[0m)?\\x1B\[32mYou ({meleeAttack}) (.*)'s ({meleeBodypart}) ({meleeDamage})(\.|!)$", RegexOptions.Compiled);
        Regex youAttackTheyParryOrDodge = new($@"^(\\x1B\[0m)?You try to ({meleeAttack}) (.*), but (he|she|it) (deflects the blow|parries successfully|dodges the attack)\.$", RegexOptions.Compiled);
        Regex youAttackTheySwiftlyDodge = new($@"^(\\x1B\[0m)?(.*) swiftly dodges your attempt to ({meleeAttack}) (him|her|it)\.$", RegexOptions.Compiled);

        Regex theyAttackYou = new($@"^(\\x1B\[0m)?\\x1B\[31m(.*) ({meleeAttacked}) your ({meleeBodypart})(\.|!)$", RegexOptions.Compiled);
        Regex theyAttackIntoYou = new($@"^(\\x1B\[0m)?\\x1B\[31m(.*) ({meleeAttacked}) your ({meleeBodypart}) ({meleeDamage})(\.|!)$", RegexOptions.Compiled);
        Regex theyBarelyYou = new($@"^(\\x1B\[0m)?\\x1B\[31m(.*) barely ({meleeAttacked}) your ({meleeBodypart})\.$", RegexOptions.Compiled);
        Regex theyTickleYou = new($@"^(\\x1B\[0m)?\\x1B\[31m(.*) (barely tickles|tickles) your ({meleeBodypart}) with (his|her|its) ({meleeAttack})\.$", RegexOptions.Compiled);
        Regex theyMissYou = new($@"^(\\x1B\[0m)?(.*) tries to ({meleeAttack}) you, but you (deflect the blow|parry successfully|dodge the attack)\.$", RegexOptions.Compiled);
        Regex theySwiftlyMissYou = new($@"^(\\x1B\[0m)?You swiftly dodge (.*)'s attempt to ({@meleeAttack}) you.$", RegexOptions.Compiled);

        Regex theyAttackThem = new($@"^(\\x1B\[0m)?(.*) ({meleeAttacked}) (.*)'s ({meleeBodypart})(\.|!)$", RegexOptions.Compiled);
        Regex theyAttackIntoThem = new($@"^(\\x1B\[0m)?(.*) ({meleeAttacked}) (.*)'s ({meleeBodypart}) ({meleeDamage})(\.|!)$", RegexOptions.Compiled);
        Regex theyBarelyThem = new($@"^(\\x1B\[0m)?(.*) barely ({meleeAttacked}) (.*)'s ({meleeBodypart})\.$", RegexOptions.Compiled);
        Regex theyTickleThem = new($@"^(\\x1B\[0m)?(.*) (barely tickles|tickles) (.*)'s ({meleeBodypart}) with (his|her|its) ({meleeAttack})\.$", RegexOptions.Compiled);
        Regex theyMissThem = new($@"^(\\x1B\[0m)?(.*) tries to ({meleeAttack}) (.*), but (he|she|it) (deflects the blow|parries successfully|dodges the attack)\.$", RegexOptions.Compiled);
        Regex theySwiftlyMissThem = new($@"^(\\x1B\[0m)?(.*) swiftly dodges (.*)'s attempt to ({@meleeAttack}) (him|her|it).$", RegexOptions.Compiled);
        internal AttackParser() {
        }

        internal bool Matches(string line) {
            var regexes = new List<Regex> {
                youTickle,
                youBarely,
                youAttack,
                youAttackInto,
                youAttackTheyParryOrDodge,
                youAttackTheySwiftlyDodge,

                theyAttackYou,
                theyAttackIntoYou,
                theyBarelyYou,
                theyTickleYou,
                theyMissYou,
                theySwiftlyMissYou,

                theyAttackThem,
                theyAttackIntoThem,
                theyBarelyThem,
                theyTickleThem,
                theyMissThem,
                theySwiftlyMissThem,
            };

            return regexes.Any(r => r.IsMatch(line));
        }
    }
}
