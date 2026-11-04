# Demo provider (`demo`)

A built-in provider for trying Lazzy without any account or token. It never touches the network.

## Token

None.

## Targets

| Target | Role |
|---|---|
| `demo:example-shop/storefront` | source control: commits, pull requests, reviews |
| `demo:example-shop/board` | work tracking: work item state changes |

Any other location under `demo:` is reported as a warning that lists these two.

## The story

The demo tells the story of a small online shop, placed in **business days before today** in your time zone,
so "yesterday" always has something to show (on a Monday, Friday's work).

| Business days ago | What happens |
|---|---|
| 5 | a commit upgrading the payment SDK |
| 3 | the "Discount codes at checkout" item moves from To Do to Doing; two commits on it |
| 2 | pull request #42 is opened; a review on a colleague's pull request #41 |
| 1 | a review on #40; a commit addressing review feedback; #42 is merged; the item moves to Ready to Test; "Show the order history" moves to Doing, with its first commit |

Weekends and configured holidays are skipped. Every name, hash and URL is fictional.
