# Codex 0.156.0's Bedrock catalog predated GPT-6 Sol/Luna, so a missing entry is
# built from its predecessor's complete, version-matched entry. 0.164.0 ships
# native GPT-6 Luna and GPT-6.1 Sol entries, so this is now only a safety net.
# This preserves the installed binary's instruction templates and tool schema;
# the request still targets the new model id. Prefer native entries when present.
def ensure_gpt6($family; $description; $priority):
    ("us.openai.gpt-6-" + $family) as $id
    | if any(.models[]; .slug == $id) then .
      else
        ([.models[] | select(.slug == ("us.openai.gpt-5.6-" + $family))][0]
            // error("No Bedrock catalog template for " + $id)) as $template
        | .models += [$template | . + {
            slug: $id,
            display_name: ("GPT-6 " + (if $family == "sol" then "Sol" else "Luna" end)
                + " (US cross-region)"),
            description: $description,
            default_reasoning_level: "medium",
            default_reasoning_summary: "none",
            priority: $priority,
            upgrade: null
        }]
      end;

if $profile == "awsgov" then
    .models |= map(select(
        .slug == "openai.gpt-5.6-terra" or .slug == "openai.gpt-5.6-luna"
    ))
else
    .models |= map(select(.slug | startswith("global.") | not))
    | if $profile == "aws" then
        ensure_gpt6("luna"; "Efficient model for focused, high-volume tasks."; 1)
        # Keep GPT-6.1 Sol (native since 0.164.0; it replaced GPT-6 Sol), GPT-6 Luna and
        # Astra, and GPT-OSS. Filter AFTER the Luna safety net, which needs its template.
        | .models |= map(select(
            .slug | test("^us\\.openai\\.gpt-(6\\.1-sol|6-luna|6-astra)$|(^|\\.)openai\\.gpt-oss-")
        ))
      else . end
end
