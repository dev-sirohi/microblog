import React from "react";
import type { Post } from "../interfaces/GlobalInterfaceExport";
import { Box, Typography, Chip, Button } from "@mui/material";
import CommentSection from "./commentsection";

interface PostProps {
    post: Post;
}

export default function PostComponent({ post }: PostProps) {
    const [showTopComments, setShowTopComments] = React.useState(false);

    return (
        <Box
            sx={{
                border: "1px solid #e0e0e0",
                p: 2,
                mb: 2,
                borderRadius: 2,
                background: "background.paper",
                boxShadow: 1,
            }}
        >
            <Box sx={{ display: "flex", justifyContent: "space-between", mb: 1 }}>
                <Typography fontWeight="bold">User {post.userId}</Typography>
                <Typography color="text.secondary" fontSize={12}>
                    {new Date(post.createdAt).toLocaleString()}
                </Typography>
            </Box>

            <Typography mb={1}>{post.content}</Typography>

            {post.medialUrl && (
                <Box
                    component="img"
                    src={post.medialUrl}
                    alt="post"
                    sx={{ width: "100%", maxHeight: 350, objectFit: "cover", borderRadius: 1, mb: 1 }}
                />
            )}

            <Box mb={1}>
                {post.tags?.map((t, i) => (
                    <Chip key={i} label={`#${t}`} size="small" sx={{ mr: 0.5, mb: 0.5 }} />
                ))}
            </Box>

            {/* Top 3 Comments Preview */}
            {post.topComments && post.comments.slice(0, 3).map((c, i) => (
                <Typography key={i} variant="body2" sx={{ mb: 0.5 }}>
                    <strong>User {c.userId}:</strong> {c.content}
                </Typography>
            ))}

            <Button size="small" onClick={() => setShowTopComments(true)}>
                View All Comments
            </Button>

            {showTopComments && <CommentSection postId={post.id} onClose={() => setShowTopComments(false)} />}
        </Box>
    );