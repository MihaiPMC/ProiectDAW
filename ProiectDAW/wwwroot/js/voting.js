function voteArticle(btn, articleId, value) {
    if (!isAuthenticated) {
        // Optional: show login prompt
        alert("Please log in to vote.");
        return;
    }

    // Optimistic UI Update
    var container = btn.closest('.vote-container');
    var scoreDisplay = container.querySelector('.score-display');
    var upBtn = container.querySelector('.upvote');
    var downBtn = container.querySelector('.downvote');

    // Store original state in case of error
    var originalScore = parseInt(scoreDisplay.innerText);
    var wasUp = upBtn.classList.contains('active-up');
    var wasDown = downBtn.classList.contains('active-down');

    // Send Request
    var token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    fetch('/News/VoteArticle', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: `articleId=${articleId}&voteValue=${value}`
    })
        .then(response => {
            if (!response.ok) throw new Error("Vote failed");
            return response.json();
        })
        .then(data => {
            // Update score from server
            scoreDisplay.innerText = data.score;
            if (value === 1) {
                if (wasUp) {
                    upBtn.classList.remove('active-up');
                } else {
                    upBtn.classList.add('active-up');
                    downBtn.classList.remove('active-down');
                }
            } else if (value === -1) {
                if (wasDown) {
                    downBtn.classList.remove('active-down');
                } else {
                    downBtn.classList.add('active-down');
                    upBtn.classList.remove('active-up');
                }
            }
        })
        .catch(err => {
            console.error(err);
            alert("Action failed.");
        });
}

function voteComment(btn, commentId, value) {
    if (!isAuthenticated) {
        alert("Please log in to vote.");
        return;
    }

    var container = btn.closest('.vote-container');
    var scoreDisplay = container.querySelector('.score-display');
    var upBtn = container.querySelector('.upvote');
    var downBtn = container.querySelector('.downvote');

    var wasUp = upBtn.classList.contains('active-up');
    var wasDown = downBtn.classList.contains('active-down');

    var token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    fetch('/News/VoteComment', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: `commentId=${commentId}&voteValue=${value}`
    })
        .then(response => {
            if (!response.ok) throw new Error("Vote failed");
            return response.json();
        })
        .then(data => {
            scoreDisplay.innerText = data.score;

            if (value === 1) {
                if (wasUp) {
                    upBtn.classList.remove('active-up');
                } else {
                    upBtn.classList.add('active-up');
                    downBtn.classList.remove('active-down');
                }
            } else if (value === -1) {
                if (wasDown) {
                    downBtn.classList.remove('active-down');
                } else {
                    downBtn.classList.add('active-down');
                    upBtn.classList.remove('active-up');
                }
            }
        })
        .catch(err => {
            console.error(err);
            alert("Action failed.");
        });
}
