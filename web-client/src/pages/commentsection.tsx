import React from "react";
import { Box, TextField, IconButton, Typography } from "@mui/material";
import { Send } from "@mui/icons-material";
import CommentApi from "../api/CommentApi";
import type { GetCommentsRequest, Comment, AddCommentRequest } from "../interfaces/GlobalInterfaceExport";

interface CommentSectionProps {
    postId: number;
    onClose: () => void;
}

export default function CommentSection({ postId, onClose }: CommentSectionProps) {
    const [comments, setComments] = React.useState<Comment[]>([]);
    const [pageNumber, setPageNumber] = React.useState(1);
    const [loading, setLoading] = React.useState(false);
    const [hasMore, setHasMore] = React.useState(true);
    const [newComment, setNewComment] = React.useState("");

    const loaderRef = React.useRef<HTMLDivElement | null>(null);

    const loadComments = async () => {
        if (loading || !hasMore) return;
        setLoading(true);
        try {
            const getCommentsRequest: GetCommentsRequest = {
                postId,
                pageNumber,
                pageSize: 10,
            };
            const res = await CommentApi.getComments(getCommentsRequest);
            if (!res.Success) {
                throw new Error(res.Message || "Unable to fetch comments");
            }
            const commentList: Comment[] = res.Data ?? [];
            if (commentList.length === 0) setHasMore(false);
            setComments(prev => [...prev, ...commentList]);
        } catch (ex) {
            console.error(ex);
        } finally {
            setLoading(false);
        }
    };

    React.useEffect(() => { loadComments(); }, [pageNumber]);

    React.useEffect(() => {
        if (!loaderRef.current || !hasMore) return;
        const observer = new IntersectionObserver((entries) => {
            if (entries[0].isIntersecting) setPageNumber(prev => prev + 1);
        });
        observer.observe(loaderRef.current);
        return () => observer.disconnect();
    }, [hasMore]);

    const addComment = async () => {
        if (!newComment.trim()) return;
        try {
            const addCommentRequest: AddCommentRequest = {
                postId,
                content: "",
            };
            const res = await CommentApi.addComment(addCommentRequest);
            if (!res.Success) {
                throw new Error(res.Message || "Unable to add comment");
            }
            const newComment: Comment = res.Data as Comment;
            setComments(prev => [newComment, ...prev]);
            setNewComment("");
        } catch (ex) {
            console.error(ex);
        }
    };

    return (
        <Box sx={{ p: 2, maxHeight: "70vh", overflowY: "auto" }}>
            <Typography variant="h6" mb={1}>Comments</Typography>

            {comments.map((c, i) => (
                <Box key={i} sx={{ mb: 1 }}>
                    <Typography variant="body2"><strong>User {c.userId}:</strong> {c.content}</Typography>
                    <Typography variant="caption" color="text.secondary">Likes: {c.likes}</Typography>
                </Box>
            ))}

            <div ref={loaderRef} />

            <Box sx={{ display: "flex", mt: 2 }}>
                <TextField
                    fullWidth
                    variant="outlined"
                    size="small"
                    placeholder="Write a comment..."
                    value={newComment}
                    onChange={(e) => setNewComment(e.target.value)}
                />
                <IconButton onClick={addComment} color="primary">
                    <Send />
                </IconButton>
            </Box>
        </Box>
    );
}
